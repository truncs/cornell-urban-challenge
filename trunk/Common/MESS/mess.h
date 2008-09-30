#ifndef MESS_H_MAY_22_2007
#define MESS_H_MAY_22_2007

// some common device-independent mess defines
// these comply to SKYNET MESS rev B

// udp settings
#define MESS_IP   0xEF840128    // 239.132.1.40 (multicast)
#define MESS_IP_STR   "239.132.1.40"     // 239.132.1.40 (multicast)
#define MESS_PORT 30040

// TYPE byte
#define MESS_TS       0x40
#define MESS_NO_TS    0x00

#define MESS_BINARY   0x20
#define MESS_TEXT     0x00

#define MESS_STARTUP  0x00
#define MESS_ERROR    0x08
#define MESS_WARNING  0x10
#define MESS_INFO     0x18

#define MESS_TYPE_0   0x00
#define MESS_TYPE_1   0x01
#define MESS_TYPE_2   0x02
#define MESS_TYPE_3   0x03
#define MESS_TYPE_4   0x04
#define MESS_TYPE_5   0x05
#define MESS_TYPE_6   0x06
#define MESS_TYPE_7   0x07

#define MESS_NAME_DECL (MESS_TEXT|MESS_INFO|MESS_TYPE_7)

#define MESS_TAG_MCU_BRIDGE   0xC0
#define MESS_TAG_MCU_INDIRECT 0x80

#endif //MESS_H_MAY_22_2007
