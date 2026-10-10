#pragma once

/*
 * Wire contract for the Poss virtual HID control device.
 * The C# copy is src/Poss.Win.Automation.VirtualHid/VhfProtocol.cs.
 * Both sides are pack(1). Change them together.
 *
 * Session:
 *   IOCTL_POSS_VHF_HELLO with Version == POSS_VHF_PROTOCOL_VERSION
 *   stores a 16-byte token on that handle and returns it.
 *   Key and mouse IOCTLs must carry the same token.
 *   A command before hello, a bad version, or a bad token is rejected.
 *   The first successful hello creates the keyboard and mouse.
 *   The last handle close removes them.
 *
 * The control device is opened from user mode as \\.\PossWinAutomationVhf.
 */

#ifdef _KERNEL_MODE
#include <ntddk.h>
#else
#include <windows.h>
#include <winioctl.h>
#endif

#define POSS_VHF_PROTOCOL_VERSION 1u
#define POSS_VHF_TOKEN_SIZE 16u

#define POSS_VHF_KEY_UP 0u
#define POSS_VHF_KEY_DOWN 1u

#define POSS_VHF_MOUSE_BUTTON_LEFT 0u
#define POSS_VHF_MOUSE_BUTTON_RIGHT 1u
#define POSS_VHF_MOUSE_BUTTON_MIDDLE 2u
#define POSS_VHF_MOUSE_BUTTON_X1 3u
#define POSS_VHF_MOUSE_BUTTON_X2 4u

#define POSS_VHF_NT_DEVICE_NAME     L"\\Device\\PossWinAutomationVhf"
#define POSS_VHF_SYMBOLIC_LINK_NAME L"\\DosDevices\\PossWinAutomationVhf"
#define POSS_VHF_USER_DEVICE_PATH   L"\\\\.\\PossWinAutomationVhf"

/* {2D27E086-51B5-4E66-ADD5-8D7FA2C8A589} */
static const GUID GUID_DEVINTERFACE_POSS_VHF =
{ 0x2d27e086, 0x51b5, 0x4e66, { 0xad, 0xd5, 0x8d, 0x7f, 0xa2, 0xc8, 0xa5, 0x89 } };

/*
 * Vendor device type 0x8000, function codes 0x800+, METHOD_BUFFERED, FILE_ANY_ACCESS.
 * IOCTL_POSS_VHF_HELLO       0x80002000
 * IOCTL_POSS_VHF_KEY         0x80002004
 * IOCTL_POSS_VHF_MOUSE_BUTTON 0x80002008
 * IOCTL_POSS_VHF_MOUSE_MOVE  0x8000200C
 * IOCTL_POSS_VHF_MOUSE_WHEEL 0x80002010
 */
#define POSS_VHF_DEVICE_TYPE 0x8000u

#define IOCTL_POSS_VHF_HELLO \
    CTL_CODE(POSS_VHF_DEVICE_TYPE, 0x800, METHOD_BUFFERED, FILE_ANY_ACCESS)

#define IOCTL_POSS_VHF_KEY \
    CTL_CODE(POSS_VHF_DEVICE_TYPE, 0x801, METHOD_BUFFERED, FILE_ANY_ACCESS)

#define IOCTL_POSS_VHF_MOUSE_BUTTON \
    CTL_CODE(POSS_VHF_DEVICE_TYPE, 0x802, METHOD_BUFFERED, FILE_ANY_ACCESS)

#define IOCTL_POSS_VHF_MOUSE_MOVE \
    CTL_CODE(POSS_VHF_DEVICE_TYPE, 0x803, METHOD_BUFFERED, FILE_ANY_ACCESS)

#define IOCTL_POSS_VHF_MOUSE_WHEEL \
    CTL_CODE(POSS_VHF_DEVICE_TYPE, 0x804, METHOD_BUFFERED, FILE_ANY_ACCESS)

#pragma pack(push, 1)

typedef struct _POSS_VHF_HELLO_REQUEST {
    UINT32 Version;
} POSS_VHF_HELLO_REQUEST, *PPOSS_VHF_HELLO_REQUEST;

typedef struct _POSS_VHF_HELLO_RESPONSE {
    UINT32 Version;
    UINT8 Token[POSS_VHF_TOKEN_SIZE];
} POSS_VHF_HELLO_RESPONSE, *PPOSS_VHF_HELLO_RESPONSE;

typedef struct _POSS_VHF_KEY_REQUEST {
    UINT8 Token[POSS_VHF_TOKEN_SIZE];
    UINT16 Usage;
    UINT8 Down;
} POSS_VHF_KEY_REQUEST, *PPOSS_VHF_KEY_REQUEST;

typedef struct _POSS_VHF_MOUSE_BUTTON_REQUEST {
    UINT8 Token[POSS_VHF_TOKEN_SIZE];
    UINT8 Button;
    UINT8 Down;
} POSS_VHF_MOUSE_BUTTON_REQUEST, *PPOSS_VHF_MOUSE_BUTTON_REQUEST;

typedef struct _POSS_VHF_MOUSE_MOVE_REQUEST {
    UINT8 Token[POSS_VHF_TOKEN_SIZE];
    INT32 DeltaX;
    INT32 DeltaY;
} POSS_VHF_MOUSE_MOVE_REQUEST, *PPOSS_VHF_MOUSE_MOVE_REQUEST;

typedef struct _POSS_VHF_MOUSE_WHEEL_REQUEST {
    UINT8 Token[POSS_VHF_TOKEN_SIZE];
    INT32 Delta;
} POSS_VHF_MOUSE_WHEEL_REQUEST, *PPOSS_VHF_MOUSE_WHEEL_REQUEST;

#pragma pack(pop)

typedef char PossVhfHelloRequestSizeCheck[(sizeof(POSS_VHF_HELLO_REQUEST) == 4) ? 1 : -1];
typedef char PossVhfHelloResponseSizeCheck[(sizeof(POSS_VHF_HELLO_RESPONSE) == 20) ? 1 : -1];
typedef char PossVhfKeyRequestSizeCheck[(sizeof(POSS_VHF_KEY_REQUEST) == 19) ? 1 : -1];
typedef char PossVhfMouseButtonRequestSizeCheck[(sizeof(POSS_VHF_MOUSE_BUTTON_REQUEST) == 18) ? 1 : -1];
typedef char PossVhfMouseMoveRequestSizeCheck[(sizeof(POSS_VHF_MOUSE_MOVE_REQUEST) == 24) ? 1 : -1];
typedef char PossVhfMouseWheelRequestSizeCheck[(sizeof(POSS_VHF_MOUSE_WHEEL_REQUEST) == 20) ? 1 : -1];
