;-----------------------------------------------------------------
; vasmm68k_mot -m68000 -Fbin -o rom_handler.bin rom_handler.s
;-----------------------------------------------------------------

_LVOOpenLibrary     EQU -552
_LVOOldOpenLibrary     EQU -408
_LVOCloseLibrary    EQU -414
_LVOFindTask        EQU -294
_LVOPutMsg          EQU -366
_LVOGetMsg          EQU -372
_LVOWaitPort        EQU -384
_LVOMakeDosNode     EQU -144
_LVOAddBootNode     EQU -36
_LVOAddDosNode		EQU -150
_LVOReplyMsg		EQU -378

BOOTPRI_NOAUTOBOOT  EQU -128
ADNF_STARTPROC      EQU 1

dn_Handler          EQU 16      ; Offset of the Handler string BPTR
dn_SegList          EQU 32      ; SegList BPTR
dn_GlobalVec        EQU 36      ; BCPL Global Vector
pr_MsgPort          EQU 92      ; Offset of MsgPort in Process struct
dp_Port             EQU 4       ; DosPacket reply port offset

    ORG     $0000

	DS.B	$40						; Zorro register area

ROM_Start:
DiagArea:
    DC.B    $90                     ; da_Config: DAC_WORDWIDE ($80) + DAC_CONFIGTIME ($10)
    DC.B    0                       ; da_Flags
    DC.W    ROM_End-ROM_Start       ; da_Size
    DC.W    DiagPoint-ROM_Start     ; da_DiagPoint
    DC.W    $FFFF                   ; da_BootPoint (Ignored)
    DC.W    DevName-ROM_Start       ; da_Name
    DC.L    0                       ; da_Reserved01/02

;-----------------------------------------------------------------
; DiagPoint
;
; A7 - points to at least 2K of stack
; A6 - ExecBase
; A5 - ExpansionBase
; A3 - board ConfigDev structure
; A2 - Base of diag/init area that was copied
; A0 - Base of board
;-----------------------------------------------------------------

DiagPoint:
    movem.l d2-d7/a2-a6,-(sp)       ; Preserve registers

	;lea $dff000,a0
	;move.w #$f00,$180(a0)

    ; --- Patch and Create DOS Node ---
    lea     DosPacket(pc),a0        ; A0 = Parameter packet
    lea     DosName(pc),a1          ; A1 = "MYDEV" string
    move.l  a1,(a0)                 ; Patch pointer 0 in packet (DOS Name)
    
    move.l  a5,a6                   ; A6 = ExpansionBase
    jsr     _LVOMakeDosNode(a6)
    tst.l   d0
    beq     .NodeFail
    move.l  d0,a4                   ; A4 = returned DeviceNode

    ; --- Calculate SegList BPTR and patch it ---
    move.l  a2,d0                   ; D0 = base
    add.l   #ROM_SegList_BPTR_Target-ROM_Start,d0 
    lsr.l   #2,d0                   ; make BPTR
    move.l  d0,dn_SegList(a4)       ; put BPTR into DeviceNode

	move.l  #-1,dn_GlobalVec(a4)    ; 68000 code not BCPL
    move.l  #0,dn_Handler(a4)

    ; --- Add BootNode to System List ---
    move.l  a4,a0                   ; A0 = DeviceNode
    ;move.l  #BOOTPRI_NOAUTOBOOT,d0  ; D0 = -128 (Do not boot)
	move.l	#0,d0					; 
    move.l  #ADNF_STARTPROC,d1      ; D1 = 1 (Spawn handler process)
    jsr     _LVOAddDosNode(a6)

.NodeFail:
 
    moveq   #1,d0                   ; success
    bra     .Exit

.InitFail:
    moveq   #0,d0                   ; failure

.Exit:
    movem.l (sp)+,d2-d7/a2-a6       ; Restore registers
    rts

;-----------------------------------------------------------------
; handler
;-----------------------------------------------------------------
    
	CNOP    0,4

ROM_SegList_Start:
	DC.L    (ROM_Handler_End-ROM_Handler_Entry)/4
ROM_SegList_BPTR_Target:
    DC.L    0                       ; Next BPTR (0 = NULL)

ROM_Handler_Entry:
    move.l  4,a6                    ; A6 = ExecBase
    
	lea $dff000,a0
	move.w #$0f0,$180(a0)

	move.l	#$00fffffe,$80			;hack trap 0 vector

    ; --- Find Process and built-in MsgPort ---
    suba.l  a1,a1                   ; A1 = NULL (find current task)
    jsr     _LVOFindTask(a6)
    move.l  d0,a3                   ; A3 = struct Process
    lea     pr_MsgPort(a3),a3       ; A3 = MsgPort

.PacketLoop:
    move.l  a3,a0                   ; A0 = MsgPort
    move.l  4,a6                    ; A6 = ExecBase
    jsr     _LVOWaitPort(a6)

.GetMessage:
    move.l  a3,a0                   ; A0 = MsgPort
    jsr     _LVOGetMsg(a6)
    tst.l   d0
    beq     .PacketLoop             ; If NULL, go back to sleep
    
    move.l  d0,a2                   ; A2 = struct Message
    move.l  10(a2),a4               ; A4 = struct DosPacket 

	; jump to C# handler
    TRAP    #0

    move.l  dp_Port(a4),a0          ; A0 = Where AmigaDOS wants the reply
    move.l  a2,a1                   ; A1 = The Exec Message
    move.l  4,a6                    ; A6 = ExecBase
    jsr     _LVOPutMsg(a6)
    
    bra     .GetMessage

ROM_Handler_End:

;-----------------------------------------------------------------

ExpLibName: DC.B    "expansion.library",0
DevName:    DC.B    "JammyDevice",0
DosName:    DC.B    "MYDEV",0

    EVEN

DosPacket:
    DC.L    0           ; [0] Pointer to DOS Name (Patched in RAM)
    DC.L    0           ; [1] Pointer to Exec Name (NULL)
    DC.L    0           ; [2] Unit number
    DC.L    0           ; [3] Open Flags
    DC.L    16          ; [4] Env Vector Table Size
    DC.L    512         ; [5] Block Size
    DC.L    0           ; [6] SecOrg
    DC.L    1           ; [7] Surfaces
    DC.L    1           ; [8] Sectors Per Block
    DC.L    1           ; [9] Blocks Per Track
    DC.L    0           ; [10] Reserved Blocks
    DC.L    0           ; [11] Prealloc
    DC.L    0           ; [12] Interleave
    DC.L    0           ; [13] LowCyl
    DC.L    0           ; [14] HighCyl
    DC.L    5           ; [15] NumBuffers
    DC.L    1           ; [16] BufMemType (MEMF_PUBLIC)
    DC.L    $00FFFFFF   ; [17] MaxTransfer 
    DC.L    $7FFFFFFE   ; [18] Mask 
    DC.L    0           ; [19] BootPri
    DC.L    $4D594653   ; [20] DosType ("MYFS")
	
ROM_End:
