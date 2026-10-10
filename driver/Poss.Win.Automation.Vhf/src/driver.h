#pragma once

#include <ntddk.h>
#include <wdf.h>
#include <vhf.h>
#include "VhfIoctl.h"

#define POSS_VHF_REPORT_QUEUE_DEPTH 64
#define POSS_VHF_REPORT_BYTES 8

typedef struct _POSS_VHF_REPORT {
    UCHAR Data[POSS_VHF_REPORT_BYTES];
    UCHAR Length;
} POSS_VHF_REPORT, *PPOSS_VHF_REPORT;

typedef struct _POSS_VHF_ENDPOINT {
    VHFHANDLE Handle;
    WDFSPINLOCK Lock;
    KEVENT Idle;
    BOOLEAN Ready;
    BOOLEAN Closing;
    ULONG SubmitInFlight;
    ULONG Head;
    ULONG Count;
    UCHAR InFlight[POSS_VHF_REPORT_BYTES];
    POSS_VHF_REPORT Pending[POSS_VHF_REPORT_QUEUE_DEPTH];
} POSS_VHF_ENDPOINT, *PPOSS_VHF_ENDPOINT;

typedef struct _DEVICE_CONTEXT {
    WDFDEVICE Device;
    WDFWAITLOCK Lock;
    ULONG SessionCount;
    UCHAR KeyboardState[POSS_VHF_REPORT_BYTES];
    UCHAR MouseButtons;
    POSS_VHF_ENDPOINT Keyboard;
    POSS_VHF_ENDPOINT Mouse;
} DEVICE_CONTEXT, *PDEVICE_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(DEVICE_CONTEXT, DeviceGetContext)

typedef struct _FILE_CONTEXT {
    BOOLEAN HelloCompleted;
    UCHAR Token[POSS_VHF_TOKEN_SIZE];
} FILE_CONTEXT, *PFILE_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(FILE_CONTEXT, FileGetContext)

NTSTATUS
HidPrepareEndpoints(
    _In_ PDEVICE_CONTEXT DeviceContext
    );

NTSTATUS
HidCreateSessionDevices(
    _In_ PDEVICE_CONTEXT DeviceContext
    );

VOID
HidDestroySessionDevices(
    _In_ PDEVICE_CONTEXT DeviceContext
    );

NTSTATUS
HidSubmitKey(
    _In_ PDEVICE_CONTEXT DeviceContext,
    _In_ USHORT Usage,
    _In_ BOOLEAN Down
    );

NTSTATUS
HidSubmitMouseButton(
    _In_ PDEVICE_CONTEXT DeviceContext,
    _In_ UCHAR Button,
    _In_ BOOLEAN Down
    );

NTSTATUS
HidSubmitMouseMove(
    _In_ PDEVICE_CONTEXT DeviceContext,
    _In_ LONG DeltaX,
    _In_ LONG DeltaY
    );

NTSTATUS
HidSubmitMouseWheel(
    _In_ PDEVICE_CONTEXT DeviceContext,
    _In_ LONG Delta
    );
