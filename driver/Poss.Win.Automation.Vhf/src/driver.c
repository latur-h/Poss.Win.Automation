#include "driver.h"

#define POSS_VHF_SDDL L"D:P(A;;GA;;;SY)(A;;GA;;;BA)(A;;GRGW;;;IU)(A;;GRGW;;;BU)"

DRIVER_INITIALIZE DriverEntry;
EVT_WDF_DRIVER_DEVICE_ADD EvtDeviceAdd;
EVT_WDF_IO_QUEUE_IO_DEVICE_CONTROL EvtIoDeviceControl;
EVT_WDF_FILE_CLEANUP EvtFileCleanup;

NTSYSAPI
ULONG
NTAPI
RtlRandomEx(
    _Inout_ PULONG Seed
    );

static BOOLEAN
TokenEquals(
    _In_reads_(POSS_VHF_TOKEN_SIZE) const UCHAR* Left,
    _In_reads_(POSS_VHF_TOKEN_SIZE) const UCHAR* Right
    )
{
    UCHAR diff = 0;
    ULONG index;

    for (index = 0; index < POSS_VHF_TOKEN_SIZE; index++) {
        diff |= (UCHAR)(Left[index] ^ Right[index]);
    }

    return diff == 0;
}

static VOID
FillToken(
    _Out_writes_(POSS_VHF_TOKEN_SIZE) UCHAR* Token
    )
{
    ULONG seed = (ULONG)KeQueryPerformanceCounter(NULL).LowPart;
    ULONG index;

    for (index = 0; index < POSS_VHF_TOKEN_SIZE; index += sizeof(ULONG)) {
        ULONG value = RtlRandomEx(&seed);
        RtlCopyMemory(Token + index, &value, sizeof(ULONG));
    }
}

static VOID
CompleteRequest(
    _In_ WDFREQUEST Request,
    _In_ NTSTATUS Status,
    _In_ ULONG_PTR Information
    )
{
    WdfRequestCompleteWithInformation(Request, Status, Information);
}

static NTSTATUS
HandleHello(
    _In_ PDEVICE_CONTEXT DeviceContext,
    _In_ PFILE_CONTEXT FileContext,
    _In_ WDFREQUEST Request,
    _Out_ ULONG_PTR* Information
    )
{
    NTSTATUS status;
    PVOID input = NULL;
    PVOID output = NULL;
    size_t inputLength = 0;
    size_t outputLength = 0;
    PPOSS_VHF_HELLO_REQUEST hello;
    PPOSS_VHF_HELLO_RESPONSE response;
    UCHAR token[POSS_VHF_TOKEN_SIZE];

    status = WdfRequestRetrieveInputBuffer(Request, sizeof(POSS_VHF_HELLO_REQUEST), &input, &inputLength);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    status = WdfRequestRetrieveOutputBuffer(Request, sizeof(POSS_VHF_HELLO_RESPONSE), &output, &outputLength);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    hello = (PPOSS_VHF_HELLO_REQUEST)input;
    response = (PPOSS_VHF_HELLO_RESPONSE)output;

    if (FileContext->HelloCompleted) {
        return STATUS_INVALID_DEVICE_STATE;
    }

    if (hello->Version != POSS_VHF_PROTOCOL_VERSION) {
        return STATUS_INVALID_PARAMETER;
    }

    FillToken(token);

    if (DeviceContext->SessionCount == 0) {
        status = HidCreateSessionDevices(DeviceContext);
        if (!NT_SUCCESS(status)) {
            return status;
        }
    }

    RtlCopyMemory(FileContext->Token, token, POSS_VHF_TOKEN_SIZE);
    FileContext->HelloCompleted = TRUE;
    DeviceContext->SessionCount++;

    RtlZeroMemory(response, sizeof(*response));
    response->Version = POSS_VHF_PROTOCOL_VERSION;
    RtlCopyMemory(response->Token, token, POSS_VHF_TOKEN_SIZE);
    *Information = sizeof(*response);
    return STATUS_SUCCESS;
}

static NTSTATUS
RequireSession(
    _In_ PFILE_CONTEXT FileContext,
    _In_reads_(POSS_VHF_TOKEN_SIZE) const UCHAR* Token
    )
{
    if (!FileContext->HelloCompleted || !TokenEquals(FileContext->Token, Token)) {
        return STATUS_ACCESS_DENIED;
    }

    return STATUS_SUCCESS;
}

static NTSTATUS
HandleKey(
    _In_ PDEVICE_CONTEXT DeviceContext,
    _In_ PFILE_CONTEXT FileContext,
    _In_ WDFREQUEST Request
    )
{
    NTSTATUS status;
    PVOID input = NULL;
    PPOSS_VHF_KEY_REQUEST key;

    status = WdfRequestRetrieveInputBuffer(Request, sizeof(POSS_VHF_KEY_REQUEST), &input, NULL);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    key = (PPOSS_VHF_KEY_REQUEST)input;
    status = RequireSession(FileContext, key->Token);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    if (key->Down != POSS_VHF_KEY_UP && key->Down != POSS_VHF_KEY_DOWN) {
        return STATUS_INVALID_PARAMETER;
    }

    return HidSubmitKey(DeviceContext, key->Usage, key->Down == POSS_VHF_KEY_DOWN);
}

static NTSTATUS
HandleMouseButton(
    _In_ PDEVICE_CONTEXT DeviceContext,
    _In_ PFILE_CONTEXT FileContext,
    _In_ WDFREQUEST Request
    )
{
    NTSTATUS status;
    PVOID input = NULL;
    PPOSS_VHF_MOUSE_BUTTON_REQUEST button;

    status = WdfRequestRetrieveInputBuffer(Request, sizeof(POSS_VHF_MOUSE_BUTTON_REQUEST), &input, NULL);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    button = (PPOSS_VHF_MOUSE_BUTTON_REQUEST)input;
    status = RequireSession(FileContext, button->Token);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    if (button->Down != POSS_VHF_KEY_UP && button->Down != POSS_VHF_KEY_DOWN) {
        return STATUS_INVALID_PARAMETER;
    }

    return HidSubmitMouseButton(DeviceContext, button->Button, button->Down == POSS_VHF_KEY_DOWN);
}

static NTSTATUS
HandleMouseMove(
    _In_ PDEVICE_CONTEXT DeviceContext,
    _In_ PFILE_CONTEXT FileContext,
    _In_ WDFREQUEST Request
    )
{
    NTSTATUS status;
    PVOID input = NULL;
    PPOSS_VHF_MOUSE_MOVE_REQUEST move;

    status = WdfRequestRetrieveInputBuffer(Request, sizeof(POSS_VHF_MOUSE_MOVE_REQUEST), &input, NULL);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    move = (PPOSS_VHF_MOUSE_MOVE_REQUEST)input;
    status = RequireSession(FileContext, move->Token);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    return HidSubmitMouseMove(DeviceContext, move->DeltaX, move->DeltaY);
}

static NTSTATUS
HandleMouseWheel(
    _In_ PDEVICE_CONTEXT DeviceContext,
    _In_ PFILE_CONTEXT FileContext,
    _In_ WDFREQUEST Request
    )
{
    NTSTATUS status;
    PVOID input = NULL;
    PPOSS_VHF_MOUSE_WHEEL_REQUEST wheel;

    status = WdfRequestRetrieveInputBuffer(Request, sizeof(POSS_VHF_MOUSE_WHEEL_REQUEST), &input, NULL);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    wheel = (PPOSS_VHF_MOUSE_WHEEL_REQUEST)input;
    status = RequireSession(FileContext, wheel->Token);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    return HidSubmitMouseWheel(DeviceContext, wheel->Delta);
}

VOID
EvtIoDeviceControl(
    _In_ WDFQUEUE Queue,
    _In_ WDFREQUEST Request,
    _In_ size_t OutputBufferLength,
    _In_ size_t InputBufferLength,
    _In_ ULONG IoControlCode
    )
{
    WDFDEVICE device = WdfIoQueueGetDevice(Queue);
    PDEVICE_CONTEXT deviceContext = DeviceGetContext(device);
    PFILE_CONTEXT fileContext = FileGetContext(WdfRequestGetFileObject(Request));
    NTSTATUS status = STATUS_INVALID_DEVICE_REQUEST;
    ULONG_PTR information = 0;

    UNREFERENCED_PARAMETER(OutputBufferLength);
    UNREFERENCED_PARAMETER(InputBufferLength);

    WdfWaitLockAcquire(deviceContext->Lock, NULL);

    switch (IoControlCode) {
    case IOCTL_POSS_VHF_HELLO:
        status = HandleHello(deviceContext, fileContext, Request, &information);
        break;
    case IOCTL_POSS_VHF_KEY:
        status = HandleKey(deviceContext, fileContext, Request);
        break;
    case IOCTL_POSS_VHF_MOUSE_BUTTON:
        status = HandleMouseButton(deviceContext, fileContext, Request);
        break;
    case IOCTL_POSS_VHF_MOUSE_MOVE:
        status = HandleMouseMove(deviceContext, fileContext, Request);
        break;
    case IOCTL_POSS_VHF_MOUSE_WHEEL:
        status = HandleMouseWheel(deviceContext, fileContext, Request);
        break;
    default:
        status = STATUS_INVALID_DEVICE_REQUEST;
        break;
    }

    WdfWaitLockRelease(deviceContext->Lock);
    CompleteRequest(Request, status, information);
}

VOID
EvtFileCleanup(
    _In_ WDFFILEOBJECT FileObject
    )
{
    WDFDEVICE device = WdfFileObjectGetDevice(FileObject);
    PDEVICE_CONTEXT deviceContext = DeviceGetContext(device);
    PFILE_CONTEXT fileContext = FileGetContext(FileObject);

    WdfWaitLockAcquire(deviceContext->Lock, NULL);

    if (fileContext->HelloCompleted) {
        fileContext->HelloCompleted = FALSE;
        if (deviceContext->SessionCount > 0) {
            deviceContext->SessionCount--;
        }

        if (deviceContext->SessionCount == 0) {
            HidDestroySessionDevices(deviceContext);
        }
    }

    WdfWaitLockRelease(deviceContext->Lock);
}

NTSTATUS
EvtDeviceAdd(
    _In_ WDFDRIVER Driver,
    _Inout_ PWDFDEVICE_INIT DeviceInit
    )
{
    NTSTATUS status;
    WDFDEVICE device;
    PDEVICE_CONTEXT deviceContext;
    WDF_OBJECT_ATTRIBUTES deviceAttributes;
    WDF_OBJECT_ATTRIBUTES fileAttributes;
    WDF_OBJECT_ATTRIBUTES lockAttributes;
    WDF_FILEOBJECT_CONFIG fileConfig;
    WDF_IO_QUEUE_CONFIG queueConfig;
    UNICODE_STRING sddl;
    UNICODE_STRING symbolicLink;

    UNREFERENCED_PARAMETER(Driver);

    WdfDeviceInitSetIoType(DeviceInit, WdfDeviceIoBuffered);
    WdfDeviceInitSetDeviceType(DeviceInit, FILE_DEVICE_UNKNOWN);

    RtlInitUnicodeString(&sddl, POSS_VHF_SDDL);
    status = WdfDeviceInitAssignSDDLString(DeviceInit, &sddl);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&fileAttributes, FILE_CONTEXT);
    fileAttributes.ExecutionLevel = WdfExecutionLevelPassive;
    WDF_FILEOBJECT_CONFIG_INIT(&fileConfig, WDF_NO_EVENT_CALLBACK, WDF_NO_EVENT_CALLBACK, EvtFileCleanup);
    WdfDeviceInitSetFileObjectConfig(DeviceInit, &fileConfig, &fileAttributes);

    WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&deviceAttributes, DEVICE_CONTEXT);
    deviceAttributes.ExecutionLevel = WdfExecutionLevelPassive;
    status = WdfDeviceCreate(&DeviceInit, &deviceAttributes, &device);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    deviceContext = DeviceGetContext(device);
    deviceContext->Device = device;

    WDF_OBJECT_ATTRIBUTES_INIT(&lockAttributes);
    lockAttributes.ParentObject = device;
    status = WdfWaitLockCreate(&lockAttributes, &deviceContext->Lock);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    status = HidPrepareEndpoints(deviceContext);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    status = WdfDeviceCreateDeviceInterface(device, &GUID_DEVINTERFACE_POSS_VHF, NULL);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    RtlInitUnicodeString(&symbolicLink, POSS_VHF_SYMBOLIC_LINK_NAME);
    status = WdfDeviceCreateSymbolicLink(device, &symbolicLink);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(&queueConfig, WdfIoQueueDispatchSequential);
    queueConfig.EvtIoDeviceControl = EvtIoDeviceControl;
    status = WdfIoQueueCreate(device, &queueConfig, WDF_NO_OBJECT_ATTRIBUTES, WDF_NO_HANDLE);
    return status;
}

NTSTATUS
DriverEntry(
    _In_ PDRIVER_OBJECT DriverObject,
    _In_ PUNICODE_STRING RegistryPath
    )
{
    WDF_DRIVER_CONFIG config;

    WDF_DRIVER_CONFIG_INIT(&config, EvtDeviceAdd);
    return WdfDriverCreate(DriverObject, RegistryPath, WDF_NO_OBJECT_ATTRIBUTES, &config, WDF_NO_HANDLE);
}
