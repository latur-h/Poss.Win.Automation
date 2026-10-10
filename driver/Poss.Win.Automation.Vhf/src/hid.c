#include "driver.h"

#define POSS_VHF_AXIS_LIMIT 127
#define POSS_VHF_WHEEL_NOTCH 120
#define POSS_VHF_VENDOR_ID 0xF00F
#define POSS_VHF_KEYBOARD_PRODUCT_ID 0x0001
#define POSS_VHF_MOUSE_PRODUCT_ID 0x0002

static const UCHAR g_KeyboardReportDescriptor[] = {
    0x05, 0x01,
    0x09, 0x06,
    0xA1, 0x01,
    0x05, 0x07,
    0x19, 0xE0,
    0x29, 0xE7,
    0x15, 0x00,
    0x25, 0x01,
    0x75, 0x01,
    0x95, 0x08,
    0x81, 0x02,
    0x95, 0x01,
    0x75, 0x08,
    0x81, 0x01,
    0x95, 0x06,
    0x75, 0x08,
    0x15, 0x00,
    0x25, 0xFF,
    0x05, 0x07,
    0x19, 0x00,
    0x29, 0xFF,
    0x81, 0x00,
    0xC0
};

static const UCHAR g_MouseReportDescriptor[] = {
    0x05, 0x01,
    0x09, 0x02,
    0xA1, 0x01,
    0x09, 0x01,
    0xA1, 0x00,
    0x05, 0x09,
    0x19, 0x01,
    0x29, 0x05,
    0x15, 0x00,
    0x25, 0x01,
    0x95, 0x05,
    0x75, 0x01,
    0x81, 0x02,
    0x95, 0x01,
    0x75, 0x03,
    0x81, 0x01,
    0x05, 0x01,
    0x09, 0x30,
    0x09, 0x31,
    0x15, 0x81,
    0x25, 0x7F,
    0x75, 0x08,
    0x95, 0x02,
    0x81, 0x06,
    0x09, 0x38,
    0x15, 0x81,
    0x25, 0x7F,
    0x75, 0x08,
    0x95, 0x01,
    0x81, 0x06,
    0xC0,
    0xC0
};

static WCHAR g_KeyboardInstance[] = L"Keyboard";
static WCHAR g_MouseInstance[] = L"Mouse";

static EVT_VHF_READY_FOR_NEXT_READ_REPORT OnKeyboardReady;
static EVT_VHF_READY_FOR_NEXT_READ_REPORT OnMouseReady;

static VOID
InitializeEndpoint(
    _Out_ PPOSS_VHF_ENDPOINT Endpoint
    )
{
    RtlZeroMemory(Endpoint, sizeof(*Endpoint));
    KeInitializeEvent(&Endpoint->Idle, NotificationEvent, TRUE);
}

static VOID
TrySubmit(
    _In_ PPOSS_VHF_ENDPOINT Endpoint
    )
{
    VHFHANDLE handle;
    HID_XFER_PACKET packet;
    POSS_VHF_REPORT report;
    NTSTATUS status;

    WdfSpinLockAcquire(Endpoint->Lock);

    if (Endpoint->Closing || Endpoint->Handle == NULL || !Endpoint->Ready || Endpoint->Count == 0) {
        WdfSpinLockRelease(Endpoint->Lock);
        return;
    }

    report = Endpoint->Pending[Endpoint->Head];
    Endpoint->Head = (Endpoint->Head + 1) % POSS_VHF_REPORT_QUEUE_DEPTH;
    Endpoint->Count--;
    Endpoint->Ready = FALSE;
    Endpoint->SubmitInFlight++;
    if (Endpoint->SubmitInFlight == 1) {
        KeClearEvent(&Endpoint->Idle);
    }

    RtlCopyMemory(Endpoint->InFlight, report.Data, report.Length);
    handle = Endpoint->Handle;
    packet.reportBuffer = Endpoint->InFlight;
    packet.reportBufferLen = report.Length;
    packet.reportId = 0;

    WdfSpinLockRelease(Endpoint->Lock);

    status = VhfReadReportSubmit(handle, &packet);

    WdfSpinLockAcquire(Endpoint->Lock);
    if (!NT_SUCCESS(status)) {
        Endpoint->Head = (Endpoint->Head + POSS_VHF_REPORT_QUEUE_DEPTH - 1) % POSS_VHF_REPORT_QUEUE_DEPTH;
        Endpoint->Pending[Endpoint->Head] = report;
        Endpoint->Count++;
        Endpoint->Ready = TRUE;
    }

    Endpoint->SubmitInFlight--;
    if (Endpoint->SubmitInFlight == 0) {
        KeSetEvent(&Endpoint->Idle, IO_NO_INCREMENT, FALSE);
    }
    WdfSpinLockRelease(Endpoint->Lock);
}

static VOID
OnReady(
    _In_ PPOSS_VHF_ENDPOINT Endpoint
    )
{
    WdfSpinLockAcquire(Endpoint->Lock);
    if (!Endpoint->Closing && Endpoint->Handle != NULL) {
        Endpoint->Ready = TRUE;
    }
    WdfSpinLockRelease(Endpoint->Lock);

    TrySubmit(Endpoint);
}

static VOID
OnKeyboardReady(
    _In_ PVOID VhfClientContext
    )
{
    OnReady((PPOSS_VHF_ENDPOINT)VhfClientContext);
}

static VOID
OnMouseReady(
    _In_ PVOID VhfClientContext
    )
{
    OnReady((PPOSS_VHF_ENDPOINT)VhfClientContext);
}

static NTSTATUS
EnqueueReport(
    _In_ PPOSS_VHF_ENDPOINT Endpoint,
    _In_reads_(Length) const UCHAR* Data,
    _In_ UCHAR Length
    )
{
    ULONG index;

    if (Length == 0 || Length > POSS_VHF_REPORT_BYTES) {
        return STATUS_INVALID_PARAMETER;
    }

    WdfSpinLockAcquire(Endpoint->Lock);
    if (Endpoint->Closing || Endpoint->Handle == NULL) {
        WdfSpinLockRelease(Endpoint->Lock);
        return STATUS_DEVICE_NOT_READY;
    }

    if (Endpoint->Count >= POSS_VHF_REPORT_QUEUE_DEPTH) {
        WdfSpinLockRelease(Endpoint->Lock);
        return STATUS_DEVICE_BUSY;
    }

    index = (Endpoint->Head + Endpoint->Count) % POSS_VHF_REPORT_QUEUE_DEPTH;
    RtlCopyMemory(Endpoint->Pending[index].Data, Data, Length);
    if (Length < POSS_VHF_REPORT_BYTES) {
        RtlZeroMemory(Endpoint->Pending[index].Data + Length, POSS_VHF_REPORT_BYTES - Length);
    }
    Endpoint->Pending[index].Length = Length;
    Endpoint->Count++;
    WdfSpinLockRelease(Endpoint->Lock);
    return STATUS_SUCCESS;
}

static BOOLEAN
HasRoom(
    _In_ PPOSS_VHF_ENDPOINT Endpoint,
    _In_ ULONG Needed
    )
{
    BOOLEAN room;

    WdfSpinLockAcquire(Endpoint->Lock);
    room = !Endpoint->Closing && Endpoint->Handle != NULL &&
        (Endpoint->Count + Needed) <= POSS_VHF_REPORT_QUEUE_DEPTH;
    WdfSpinLockRelease(Endpoint->Lock);
    return room;
}

static ULONG
ChunkCount(
    _In_ LONG Value
    )
{
    LONGLONG magnitude = Value >= 0 ? (LONGLONG)Value : -(LONGLONG)Value;
    return (ULONG)((magnitude + (POSS_VHF_AXIS_LIMIT - 1)) / POSS_VHF_AXIS_LIMIT);
}

static LONG
ClampAxis(
    _In_ LONG Value
    )
{
    if (Value > POSS_VHF_AXIS_LIMIT) {
        return POSS_VHF_AXIS_LIMIT;
    }

    if (Value < -POSS_VHF_AXIS_LIMIT) {
        return -POSS_VHF_AXIS_LIMIT;
    }

    return Value;
}

static NTSTATUS
StartEndpoint(
    _In_ PPOSS_VHF_ENDPOINT Endpoint,
    _In_ PDEVICE_OBJECT DeviceObject,
    _In_reads_(DescriptorLength) const UCHAR* Descriptor,
    _In_ USHORT DescriptorLength,
    _In_ USHORT ProductId,
    _In_ PWSTR InstanceId,
    _In_ USHORT InstanceIdBytes,
    _In_ PEVT_VHF_READY_FOR_NEXT_READ_REPORT Ready
    )
{
    VHF_CONFIG config;
    NTSTATUS status;

    Endpoint->Handle = NULL;
    Endpoint->Ready = FALSE;
    Endpoint->Closing = FALSE;
    Endpoint->SubmitInFlight = 0;
    Endpoint->Head = 0;
    Endpoint->Count = 0;
    KeSetEvent(&Endpoint->Idle, IO_NO_INCREMENT, FALSE);

    VHF_CONFIG_INIT(&config, DeviceObject, DescriptorLength, (PUCHAR)Descriptor);
    config.VhfClientContext = Endpoint;
    config.VendorID = POSS_VHF_VENDOR_ID;
    config.ProductID = ProductId;
    config.VersionNumber = 0x0100;
    config.InstanceID = InstanceId;
    config.InstanceIDLength = InstanceIdBytes;
    config.EvtVhfReadyForNextReadReport = Ready;

    status = VhfCreate(&config, &Endpoint->Handle);
    if (!NT_SUCCESS(status)) {
        Endpoint->Handle = NULL;
        return status;
    }

    status = VhfStart(Endpoint->Handle);
    if (!NT_SUCCESS(status)) {
        VhfDelete(Endpoint->Handle, TRUE);
        Endpoint->Handle = NULL;
        return status;
    }

    return STATUS_SUCCESS;
}

static VOID
WaitUntilIdle(
    _In_ PPOSS_VHF_ENDPOINT Endpoint
    )
{
    for (;;) {
        BOOLEAN busy;

        WdfSpinLockAcquire(Endpoint->Lock);
        busy = Endpoint->SubmitInFlight != 0;
        WdfSpinLockRelease(Endpoint->Lock);

        if (!busy) {
            return;
        }

        KeWaitForSingleObject(&Endpoint->Idle, Executive, KernelMode, FALSE, NULL);
    }
}

static VOID
DestroyEndpoint(
    _In_ PPOSS_VHF_ENDPOINT Endpoint
    )
{
    VHFHANDLE handle;

    WdfSpinLockAcquire(Endpoint->Lock);
    Endpoint->Closing = TRUE;
    handle = Endpoint->Handle;
    Endpoint->Handle = NULL;
    WdfSpinLockRelease(Endpoint->Lock);

    WaitUntilIdle(Endpoint);

    if (handle != NULL) {
        VhfDelete(handle, TRUE);
    }

    WdfSpinLockAcquire(Endpoint->Lock);
    Endpoint->Ready = FALSE;
    Endpoint->Closing = FALSE;
    Endpoint->Head = 0;
    Endpoint->Count = 0;
    WdfSpinLockRelease(Endpoint->Lock);
}

NTSTATUS
HidPrepareEndpoints(
    _In_ PDEVICE_CONTEXT DeviceContext
    )
{
    NTSTATUS status;
    WDF_OBJECT_ATTRIBUTES attributes;

    InitializeEndpoint(&DeviceContext->Keyboard);
    InitializeEndpoint(&DeviceContext->Mouse);

    WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
    attributes.ParentObject = DeviceContext->Device;

    status = WdfSpinLockCreate(&attributes, &DeviceContext->Keyboard.Lock);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    status = WdfSpinLockCreate(&attributes, &DeviceContext->Mouse.Lock);
    return status;
}

NTSTATUS
HidCreateSessionDevices(
    _In_ PDEVICE_CONTEXT DeviceContext
    )
{
    NTSTATUS status;
    PDEVICE_OBJECT deviceObject = WdfDeviceWdmGetDeviceObject(DeviceContext->Device);

    RtlZeroMemory(DeviceContext->KeyboardState, sizeof(DeviceContext->KeyboardState));
    DeviceContext->MouseButtons = 0;

    status = StartEndpoint(
        &DeviceContext->Keyboard,
        deviceObject,
        g_KeyboardReportDescriptor,
        sizeof(g_KeyboardReportDescriptor),
        POSS_VHF_KEYBOARD_PRODUCT_ID,
        g_KeyboardInstance,
        sizeof(g_KeyboardInstance),
        OnKeyboardReady);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    status = StartEndpoint(
        &DeviceContext->Mouse,
        deviceObject,
        g_MouseReportDescriptor,
        sizeof(g_MouseReportDescriptor),
        POSS_VHF_MOUSE_PRODUCT_ID,
        g_MouseInstance,
        sizeof(g_MouseInstance),
        OnMouseReady);
    if (!NT_SUCCESS(status)) {
        DestroyEndpoint(&DeviceContext->Keyboard);
        return status;
    }

    return STATUS_SUCCESS;
}

VOID
HidDestroySessionDevices(
    _In_ PDEVICE_CONTEXT DeviceContext
    )
{
    DestroyEndpoint(&DeviceContext->Keyboard);
    DestroyEndpoint(&DeviceContext->Mouse);
    RtlZeroMemory(DeviceContext->KeyboardState, sizeof(DeviceContext->KeyboardState));
    DeviceContext->MouseButtons = 0;
}

NTSTATUS
HidSubmitKey(
    _In_ PDEVICE_CONTEXT DeviceContext,
    _In_ USHORT Usage,
    _In_ BOOLEAN Down
    )
{
    UCHAR* state = DeviceContext->KeyboardState;
    NTSTATUS status;

    if (!HasRoom(&DeviceContext->Keyboard, 1)) {
        return STATUS_DEVICE_BUSY;
    }

    if (Usage >= 0xE0 && Usage <= 0xE7) {
        UCHAR bit = (UCHAR)(1u << (Usage - 0xE0));
        if (Down) {
            state[0] |= bit;
        }
        else {
            state[0] &= (UCHAR)~bit;
        }
    }
    else if (Usage == 0 || Usage > 0xFF) {
        return STATUS_INVALID_PARAMETER;
    }
    else {
        UCHAR value = (UCHAR)Usage;
        ULONG existing = POSS_VHF_REPORT_QUEUE_DEPTH;
        ULONG empty = POSS_VHF_REPORT_QUEUE_DEPTH;
        ULONG index;

        for (index = 0; index < 6; index++) {
            if (state[2 + index] == value) {
                existing = index;
            }
            else if (state[2 + index] == 0 && empty == POSS_VHF_REPORT_QUEUE_DEPTH) {
                empty = index;
            }
        }

        if (Down) {
            if (existing == POSS_VHF_REPORT_QUEUE_DEPTH) {
                if (empty == POSS_VHF_REPORT_QUEUE_DEPTH) {
                    return STATUS_INSUFFICIENT_RESOURCES;
                }

                state[2 + empty] = value;
            }
        }
        else if (existing != POSS_VHF_REPORT_QUEUE_DEPTH) {
            for (index = existing; index < 5; index++) {
                state[2 + index] = state[3 + index];
            }

            state[7] = 0;
        }
    }

    status = EnqueueReport(&DeviceContext->Keyboard, state, POSS_VHF_REPORT_BYTES);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    TrySubmit(&DeviceContext->Keyboard);
    return STATUS_SUCCESS;
}

NTSTATUS
HidSubmitMouseButton(
    _In_ PDEVICE_CONTEXT DeviceContext,
    _In_ UCHAR Button,
    _In_ BOOLEAN Down
    )
{
    UCHAR report[4];
    NTSTATUS status;

    if (Button > POSS_VHF_MOUSE_BUTTON_X2) {
        return STATUS_INVALID_PARAMETER;
    }

    if (!HasRoom(&DeviceContext->Mouse, 1)) {
        return STATUS_DEVICE_BUSY;
    }

    if (Down) {
        DeviceContext->MouseButtons |= (UCHAR)(1u << Button);
    }
    else {
        DeviceContext->MouseButtons &= (UCHAR)~(1u << Button);
    }

    RtlZeroMemory(report, sizeof(report));
    report[0] = DeviceContext->MouseButtons;
    status = EnqueueReport(&DeviceContext->Mouse, report, sizeof(report));
    if (!NT_SUCCESS(status)) {
        return status;
    }

    TrySubmit(&DeviceContext->Mouse);
    return STATUS_SUCCESS;
}

NTSTATUS
HidSubmitMouseMove(
    _In_ PDEVICE_CONTEXT DeviceContext,
    _In_ LONG DeltaX,
    _In_ LONG DeltaY
    )
{
    ULONG needed;
    LONG remainingX = DeltaX;
    LONG remainingY = DeltaY;
    NTSTATUS status = STATUS_SUCCESS;

    if (DeltaX == 0 && DeltaY == 0) {
        return STATUS_SUCCESS;
    }

    needed = ChunkCount(DeltaX);
    if (ChunkCount(DeltaY) > needed) {
        needed = ChunkCount(DeltaY);
    }

    if (!HasRoom(&DeviceContext->Mouse, needed)) {
        return STATUS_DEVICE_BUSY;
    }

    while (remainingX != 0 || remainingY != 0) {
        UCHAR report[4];
        LONG stepX = ClampAxis(remainingX);
        LONG stepY = ClampAxis(remainingY);

        RtlZeroMemory(report, sizeof(report));
        report[0] = DeviceContext->MouseButtons;
        report[1] = (UCHAR)(CHAR)stepX;
        report[2] = (UCHAR)(CHAR)stepY;
        status = EnqueueReport(&DeviceContext->Mouse, report, sizeof(report));
        if (!NT_SUCCESS(status)) {
            return status;
        }

        remainingX -= stepX;
        remainingY -= stepY;
    }

    TrySubmit(&DeviceContext->Mouse);
    return status;
}

NTSTATUS
HidSubmitMouseWheel(
    _In_ PDEVICE_CONTEXT DeviceContext,
    _In_ LONG Delta
    )
{
    LONG detents;
    LONG remaining;
    ULONG needed;
    NTSTATUS status = STATUS_SUCCESS;

    if (Delta == 0) {
        return STATUS_SUCCESS;
    }

    detents = Delta / POSS_VHF_WHEEL_NOTCH;
    if (detents == 0) {
        detents = Delta > 0 ? 1 : -1;
    }

    needed = ChunkCount(detents);
    if (!HasRoom(&DeviceContext->Mouse, needed)) {
        return STATUS_DEVICE_BUSY;
    }

    remaining = detents;
    while (remaining != 0) {
        UCHAR report[4];
        LONG step = ClampAxis(remaining);

        RtlZeroMemory(report, sizeof(report));
        report[0] = DeviceContext->MouseButtons;
        report[3] = (UCHAR)(CHAR)step;
        status = EnqueueReport(&DeviceContext->Mouse, report, sizeof(report));
        if (!NT_SUCCESS(status)) {
            return status;
        }

        remaining -= step;
    }

    TrySubmit(&DeviceContext->Mouse);
    return status;
}
