// Generated by ARM internal tool
// DO NOT EDIT

// Copyright 2017 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// package arm64asm -- go2cs converted at 2022 March 13 06:37:48 UTC
// import "cmd/vendor/golang.org/x/arch/arm64/arm64asm" ==> using arm64asm = go.cmd.vendor.golang.org.x.arch.arm64.arm64asm_package
// Original source: C:\Program Files\Go\src\cmd\vendor\golang.org\x\arch\arm64\arm64asm\arg.go
namespace go.cmd.vendor.golang.org.x.arch.arm64;

public static partial class arm64asm_package {

// Naming for Go decoder arguments:
//
// - arg_Wd: a W register encoded in the Rd[4:0] field (31 is wzr)
//
// - arg_Xd: a X register encoded in the Rd[4:0] field (31 is xzr)
//
// - arg_Wds: a W register encoded in the Rd[4:0] field (31 is wsp)
//
// - arg_Xds: a X register encoded in the Rd[4:0] field (31 is sp)
//
// - arg_Wn: encoded in Rn[9:5]
//
// - arg_Wm: encoded in Rm[20:16]
//
// - arg_Wm_extend__UXTB_0__UXTH_1__LSL_UXTW_2__UXTX_3__SXTB_4__SXTH_5__SXTW_6__SXTX_7__0_4:
//     a W register encoded in Rm with an extend encoded in option[15:13] and an amount
//     encoded in imm3[12:10] in the range [0,4].
//
// - arg_Rm_extend__UXTB_0__UXTH_1__UXTW_2__LSL_UXTX_3__SXTB_4__SXTH_5__SXTW_6__SXTX_7__0_4:
//     a W or X register encoded in Rm with an extend encoded in option[15:13] and an
//     amount encoded in imm3[12:10] in the range [0,4]. If the extend is UXTX or SXTX,
//     it's an X register else, it's a W register.
//
// - arg_Wm_shift__LSL_0__LSR_1__ASR_2__0_31:
//     a W register encoded in Rm with a shift encoded in shift[23:22] and an amount
//     encoded in imm6[15:10] in the range [0,31].
//
// - arg_IAddSub:
//     An immediate for a add/sub instruction encoded in imm12[21:10] with an optional
//     left shift of 12 encoded in shift[23:22].
//
// - arg_Rt_31_1__W_0__X_1:
//     a W or X register encoded in Rt[4:0]. The width specifier is encoded in the field
//     [31:31] (offset 31, bit count 1) and the register is W for 0 and X for 1.
//
// - arg_[s|u]label_FIELDS_POWER:
//     a program label encoded as "FIELDS" times 2^POWER in the range [MIN, MAX] (determined
//     by signd/unsigned, FIELDS and POWER), e.g.
//       arg_slabel_imm14_2
//       arg_slabel_imm19_2
//       arg_slabel_imm26_2
//       arg_slabel_immhi_immlo_0
//       arg_slabel_immhi_immlo_12
//
// - arg_Xns_mem_post_imm7_8_signed:
//     addressing mode of post-index with a base register: Xns and a signed offset encoded
//     in the "imm7" field times 8
//
// - arg_Xns_mem_extend_m__UXTW_2__LSL_3__SXTW_6__SXTX_7__0_0__3_1:
//     addressing mode of extended register with a base register: Xns, an offset register
//     (<Wm>|<Xm>) with an extend encoded in option[15:13] and a shift amount encoded in
//     S[12:12] in the range [0,3] (S=0:0, S=1:3).
//
// - arg_Xns_mem_optional_imm12_4_unsigned:
//     addressing mode of unsigned offset with a base register: Xns and an optional unsigned
//     offset encoded in the "imm12" field times 4
//
// - arg_Xns_mem_wb_imm7_4_signed:
//     addressing mode of pre-index with a base register: Xns and the signed offset encoded
//     in the "imm7" field times 4
//
// - arg_Xns_mem_post_size_1_8_unsigned__4_0__8_1__16_2__32_3:
//     a post-index immediate offset, encoded in the "size" field. It can have the following values:
//       #4 when size = 00
//       #8 when size = 01
//       #16 when size = 10
//       #32 when size = 11
//
// - arg_immediate_0_127_CRm_op2:
//     an immediate encoded in "CRm:op2" in the range 0 to 127
//
// - arg_immediate_bitmask_64_N_imms_immr:
//     a bitmask immediate for 64-bit variant and encoded in "N:imms:immr"
//
// - arg_immediate_SBFX_SBFM_64M_bitfield_width_64_imms:
//     an immediate for the <width> bitfield of SBFX 64-bit variant
//
// - arg_immediate_shift_32_implicit_inverse_imm16_hw:
//     a 32-bit immediate of the bitwise inverse of which can be encoded in "imm16:hw"
//
// - arg_cond_[Not]AllowALNV_[Invert|Normal]:
//     a standard condition, encoded in the "cond" field, excluding (NotAllow) AL and NV with
//     its least significant bit [Yes|No] inverted, e.g.
//       arg_cond_AllowALNV_Normal
//       arg_cond_NotAllowALNV_Invert
//
// - arg_immediate_OptLSL_amount_16_0_48:
//     An immediate for MOV[KNZ] instruction encoded in imm16[20:5] with an optional
//     left shift of 16 in the range [0, 48] encoded in hw[22, 21]
//
// - arg_immediate_0_width_m1_immh_immb__UIntimmhimmb8_1__UIntimmhimmb16_2__UIntimmhimmb32_4__UIntimmhimmb64_8:
//     the left shift amount, in the range 0 to the operand width in bits minus 1,
//     encoded in the "immh:immb" field. It can have the following values:
//       (UInt(immh:immb)-8) when immh = 0001
//       (UInt(immh:immb)-16) when immh = 001x
//       (UInt(immh:immb)-32) when immh = 01xx
//       (UInt(immh:immb)-64) when immh = 1xxx
//
// - arg_immediate_1_width_immh_immb__16UIntimmhimmb_1__32UIntimmhimmb_2__64UIntimmhimmb_4:
//     the right shift amount, in the range 1 to the destination operand width in
//     bits, encoded in the "immh:immb" field. It can have the following values:
//       (16-UInt(immh:immb)) when immh = 0001
//       (32-UInt(immh:immb)) when immh = 001x
//       (64-UInt(immh:immb)) when immh = 01xx
//
// - arg_immediate_8x8_a_b_c_d_e_f_g_h:
//     a 64-bit immediate 'aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh',
//     encoded in "a:b:c:d:e:f:g:h".
//
// - arg_immediate_fbits_min_1_max_32_sub_64_scale:
//     the number of bits after the binary point in the fixed-point destination,
//     in the range 1 to 32, encoded as 64 minus "scale".
//
// - arg_immediate_floatzero: #0.0
//
// - arg_immediate_exp_3_pre_4_a_b_c_d_e_f_g_h:
//     a signed floating-point constant with 3-bit exponent and normalized 4 bits of precision,
//     encoded in "a:b:c:d:e:f:g:h"
//
// - arg_immediate_fbits_min_1_max_0_sub_0_immh_immb__64UIntimmhimmb_4__128UIntimmhimmb_8:
//     the number of fractional bits, in the range 1 to the operand width, encoded
//     in the "immh:immb" field. It can have the following values:
//       (64-UInt(immh:immb)) when immh = 01xx
//       (128-UInt(immh:immb)) when immh = 1xxx
//
// - arg_immediate_index_Q_imm4__imm4lt20gt_00__imm4_10:
//     the lowest numbered byte element to be extracted, encoded in the "Q:imm4" field.
//     It can have the following values:
//       imm4<2:0> when Q = 0, imm4<3> = 0
//       imm4 when Q = 1, imm4<3> = x
//
// - arg_sysop_AT_SYS_CR_system:
//     system operation for system instruction: AT encoded in the "op1:CRm<0>:op2" field
//
// - arg_prfop_Rt:
//     prefectch operation encoded in the "Rt"
//
// - arg_sysreg_o0_op1_CRn_CRm_op2:
//     system register name encoded in the "o0:op1:CRn:CRm:op2"
//
// - arg_pstatefield_op1_op2__SPSel_05__DAIFSet_36__DAIFClr_37:
//     PSTATE field name encoded in the "op1:op2" field
//
// - arg_Vd_arrangement_size_Q___8B_00__16B_01__4H_10__8H_11__2S_20__4S_21__2D_31:
//     one register with arrangement specifier encoded in the "size:Q" field which can have the following values:
//       8B when size = 00, Q = 0
//       16B when size = 00, Q = 1
//       4H when size = 01, Q = 0
//       8H when size = 01, Q = 1
//       2S when size = 10, Q = 0
//       4S when size = 10, Q = 1
//       2D when size = 11, Q = 1
//       The encoding size = 11, Q = 0 is reserved.
//
// - arg_Vt_3_arrangement_size_Q___8B_00__16B_01__4H_10__8H_11__2S_20__4S_21__1D_30__2D_31:
//     three registers with arrangement specifier encoded in the "size:Q" field which can have the following values:
//       8B when size = 00, Q = 0
//       16B when size = 00, Q = 1
//       4H when size = 01, Q = 0
//       8H when size = 01, Q = 1
//       2S when size = 10, Q = 0
//       4S when size = 10, Q = 1
//       2D when size = 11, Q = 1
//       The encoding size = 11, Q = 0 is reserved.
//
// - arg_Vt_1_arrangement_H_index__Q_S_size_1:
//     one register with arrangement:H and element index encoded in "Q:S:size<1>".

private partial struct instArg { // : ushort
}

private static readonly instArg _ = iota;
private static readonly var arg_Bt = 0;
private static readonly var arg_Cm = 1;
private static readonly var arg_Cn = 2;
private static readonly var arg_cond_AllowALNV_Normal = 3;
private static readonly var arg_conditional = 4;
private static readonly var arg_cond_NotAllowALNV_Invert = 5;
private static readonly var arg_Da = 6;
private static readonly var arg_Dd = 7;
private static readonly var arg_Dm = 8;
private static readonly var arg_Dn = 9;
private static readonly var arg_Dt = 10;
private static readonly var arg_Dt2 = 11;
private static readonly var arg_Hd = 12;
private static readonly var arg_Hn = 13;
private static readonly var arg_Ht = 14;
private static readonly var arg_IAddSub = 15;
private static readonly var arg_immediate_0_127_CRm_op2 = 16;
private static readonly var arg_immediate_0_15_CRm = 17;
private static readonly var arg_immediate_0_15_nzcv = 18;
private static readonly var arg_immediate_0_31_imm5 = 19;
private static readonly var arg_immediate_0_31_immr = 20;
private static readonly var arg_immediate_0_31_imms = 21;
private static readonly var arg_immediate_0_63_b5_b40 = 22;
private static readonly var arg_immediate_0_63_immh_immb__UIntimmhimmb64_8 = 23;
private static readonly var arg_immediate_0_63_immr = 24;
private static readonly var arg_immediate_0_63_imms = 25;
private static readonly var arg_immediate_0_65535_imm16 = 26;
private static readonly var arg_immediate_0_7_op1 = 27;
private static readonly var arg_immediate_0_7_op2 = 28;
private static readonly var arg_immediate_0_width_immh_immb__SEEAdvancedSIMDmodifiedimmediate_0__UIntimmhimmb8_1__UIntimmhimmb16_2__UIntimmhimmb32_4 = 29;
private static readonly var arg_immediate_0_width_immh_immb__SEEAdvancedSIMDmodifiedimmediate_0__UIntimmhimmb8_1__UIntimmhimmb16_2__UIntimmhimmb32_4__UIntimmhimmb64_8 = 30;
private static readonly var arg_immediate_0_width_m1_immh_immb__UIntimmhimmb8_1__UIntimmhimmb16_2__UIntimmhimmb32_4__UIntimmhimmb64_8 = 31;
private static readonly var arg_immediate_0_width_size__8_0__16_1__32_2 = 32;
private static readonly var arg_immediate_1_64_immh_immb__128UIntimmhimmb_8 = 33;
private static readonly var arg_immediate_1_width_immh_immb__16UIntimmhimmb_1__32UIntimmhimmb_2__64UIntimmhimmb_4 = 34;
private static readonly var arg_immediate_1_width_immh_immb__SEEAdvancedSIMDmodifiedimmediate_0__16UIntimmhimmb_1__32UIntimmhimmb_2__64UIntimmhimmb_4 = 35;
private static readonly var arg_immediate_1_width_immh_immb__SEEAdvancedSIMDmodifiedimmediate_0__16UIntimmhimmb_1__32UIntimmhimmb_2__64UIntimmhimmb_4__128UIntimmhimmb_8 = 36;
private static readonly var arg_immediate_8x8_a_b_c_d_e_f_g_h = 37;
private static readonly var arg_immediate_ASR_SBFM_32M_bitfield_0_31_immr = 38;
private static readonly var arg_immediate_ASR_SBFM_64M_bitfield_0_63_immr = 39;
private static readonly var arg_immediate_BFI_BFM_32M_bitfield_lsb_32_immr = 40;
private static readonly var arg_immediate_BFI_BFM_32M_bitfield_width_32_imms = 41;
private static readonly var arg_immediate_BFI_BFM_64M_bitfield_lsb_64_immr = 42;
private static readonly var arg_immediate_BFI_BFM_64M_bitfield_width_64_imms = 43;
private static readonly var arg_immediate_BFXIL_BFM_32M_bitfield_lsb_32_immr = 44;
private static readonly var arg_immediate_BFXIL_BFM_32M_bitfield_width_32_imms = 45;
private static readonly var arg_immediate_BFXIL_BFM_64M_bitfield_lsb_64_immr = 46;
private static readonly var arg_immediate_BFXIL_BFM_64M_bitfield_width_64_imms = 47;
private static readonly var arg_immediate_bitmask_32_imms_immr = 48;
private static readonly var arg_immediate_bitmask_64_N_imms_immr = 49;
private static readonly var arg_immediate_exp_3_pre_4_a_b_c_d_e_f_g_h = 50;
private static readonly var arg_immediate_exp_3_pre_4_imm8 = 51;
private static readonly var arg_immediate_fbits_min_1_max_0_sub_0_immh_immb__64UIntimmhimmb_4__128UIntimmhimmb_8 = 52;
private static readonly var arg_immediate_fbits_min_1_max_0_sub_0_immh_immb__SEEAdvancedSIMDmodifiedimmediate_0__64UIntimmhimmb_4__128UIntimmhimmb_8 = 53;
private static readonly var arg_immediate_fbits_min_1_max_32_sub_64_scale = 54;
private static readonly var arg_immediate_fbits_min_1_max_64_sub_64_scale = 55;
private static readonly var arg_immediate_floatzero = 56;
private static readonly var arg_immediate_index_Q_imm4__imm4lt20gt_00__imm4_10 = 57;
private static readonly var arg_immediate_LSL_UBFM_32M_bitfield_0_31_immr = 58;
private static readonly var arg_immediate_LSL_UBFM_64M_bitfield_0_63_immr = 59;
private static readonly var arg_immediate_LSR_UBFM_32M_bitfield_0_31_immr = 60;
private static readonly var arg_immediate_LSR_UBFM_64M_bitfield_0_63_immr = 61;
private static readonly var arg_immediate_MSL__a_b_c_d_e_f_g_h_cmode__8_0__16_1 = 62;
private static readonly var arg_immediate_optional_0_15_CRm = 63;
private static readonly var arg_immediate_optional_0_65535_imm16 = 64;
private static readonly var arg_immediate_OptLSL__a_b_c_d_e_f_g_h_cmode__0_0__8_1 = 65;
private static readonly var arg_immediate_OptLSL__a_b_c_d_e_f_g_h_cmode__0_0__8_1__16_2__24_3 = 66;
private static readonly var arg_immediate_OptLSL_amount_16_0_16 = 67;
private static readonly var arg_immediate_OptLSL_amount_16_0_48 = 68;
private static readonly var arg_immediate_OptLSLZero__a_b_c_d_e_f_g_h = 69;
private static readonly var arg_immediate_SBFIZ_SBFM_32M_bitfield_lsb_32_immr = 70;
private static readonly var arg_immediate_SBFIZ_SBFM_32M_bitfield_width_32_imms = 71;
private static readonly var arg_immediate_SBFIZ_SBFM_64M_bitfield_lsb_64_immr = 72;
private static readonly var arg_immediate_SBFIZ_SBFM_64M_bitfield_width_64_imms = 73;
private static readonly var arg_immediate_SBFX_SBFM_32M_bitfield_lsb_32_immr = 74;
private static readonly var arg_immediate_SBFX_SBFM_32M_bitfield_width_32_imms = 75;
private static readonly var arg_immediate_SBFX_SBFM_64M_bitfield_lsb_64_immr = 76;
private static readonly var arg_immediate_SBFX_SBFM_64M_bitfield_width_64_imms = 77;
private static readonly var arg_immediate_shift_32_implicit_imm16_hw = 78;
private static readonly var arg_immediate_shift_32_implicit_inverse_imm16_hw = 79;
private static readonly var arg_immediate_shift_64_implicit_imm16_hw = 80;
private static readonly var arg_immediate_shift_64_implicit_inverse_imm16_hw = 81;
private static readonly var arg_immediate_UBFIZ_UBFM_32M_bitfield_lsb_32_immr = 82;
private static readonly var arg_immediate_UBFIZ_UBFM_32M_bitfield_width_32_imms = 83;
private static readonly var arg_immediate_UBFIZ_UBFM_64M_bitfield_lsb_64_immr = 84;
private static readonly var arg_immediate_UBFIZ_UBFM_64M_bitfield_width_64_imms = 85;
private static readonly var arg_immediate_UBFX_UBFM_32M_bitfield_lsb_32_immr = 86;
private static readonly var arg_immediate_UBFX_UBFM_32M_bitfield_width_32_imms = 87;
private static readonly var arg_immediate_UBFX_UBFM_64M_bitfield_lsb_64_immr = 88;
private static readonly var arg_immediate_UBFX_UBFM_64M_bitfield_width_64_imms = 89;
private static readonly var arg_immediate_zero = 90;
private static readonly var arg_option_DMB_BO_system_CRm = 91;
private static readonly var arg_option_DSB_BO_system_CRm = 92;
private static readonly var arg_option_ISB_BI_system_CRm = 93;
private static readonly var arg_prfop_Rt = 94;
private static readonly var arg_pstatefield_op1_op2__SPSel_05__DAIFSet_36__DAIFClr_37 = 95;
private static readonly var arg_Qd = 96;
private static readonly var arg_Qn = 97;
private static readonly var arg_Qt = 98;
private static readonly var arg_Qt2 = 99;
private static readonly var arg_Rm_extend__UXTB_0__UXTH_1__UXTW_2__LSL_UXTX_3__SXTB_4__SXTH_5__SXTW_6__SXTX_7__0_4 = 100;
private static readonly var arg_Rn_16_5__W_1__W_2__W_4__X_8 = 101;
private static readonly var arg_Rt_31_1__W_0__X_1 = 102;
private static readonly var arg_Sa = 103;
private static readonly var arg_Sd = 104;
private static readonly var arg_slabel_imm14_2 = 105;
private static readonly var arg_slabel_imm19_2 = 106;
private static readonly var arg_slabel_imm26_2 = 107;
private static readonly var arg_slabel_immhi_immlo_0 = 108;
private static readonly var arg_slabel_immhi_immlo_12 = 109;
private static readonly var arg_Sm = 110;
private static readonly var arg_Sn = 111;
private static readonly var arg_St = 112;
private static readonly var arg_St2 = 113;
private static readonly var arg_sysop_AT_SYS_CR_system = 114;
private static readonly var arg_sysop_DC_SYS_CR_system = 115;
private static readonly var arg_sysop_IC_SYS_CR_system = 116;
private static readonly var arg_sysop_SYS_CR_system = 117;
private static readonly var arg_sysop_TLBI_SYS_CR_system = 118;
private static readonly var arg_sysreg_o0_op1_CRn_CRm_op2 = 119;
private static readonly var arg_Vd_16_5__B_1__H_2__S_4__D_8 = 120;
private static readonly var arg_Vd_19_4__B_1__H_2__S_4 = 121;
private static readonly var arg_Vd_19_4__B_1__H_2__S_4__D_8 = 122;
private static readonly var arg_Vd_19_4__D_8 = 123;
private static readonly var arg_Vd_19_4__S_4__D_8 = 124;
private static readonly var arg_Vd_22_1__S_0 = 125;
private static readonly var arg_Vd_22_1__S_0__D_1 = 126;
private static readonly var arg_Vd_22_1__S_1 = 127;
private static readonly var arg_Vd_22_2__B_0__H_1__S_2 = 128;
private static readonly var arg_Vd_22_2__B_0__H_1__S_2__D_3 = 129;
private static readonly var arg_Vd_22_2__D_3 = 130;
private static readonly var arg_Vd_22_2__H_0__S_1__D_2 = 131;
private static readonly var arg_Vd_22_2__H_1__S_2 = 132;
private static readonly var arg_Vd_22_2__S_1__D_2 = 133;
private static readonly var arg_Vd_arrangement_16B = 134;
private static readonly var arg_Vd_arrangement_2D = 135;
private static readonly var arg_Vd_arrangement_4S = 136;
private static readonly var arg_Vd_arrangement_D_index__1 = 137;
private static readonly var arg_Vd_arrangement_imm5___B_1__H_2__S_4__D_8_index__imm5__imm5lt41gt_1__imm5lt42gt_2__imm5lt43gt_4__imm5lt4gt_8_1 = 138;
private static readonly var arg_Vd_arrangement_imm5_Q___8B_10__16B_11__4H_20__8H_21__2S_40__4S_41__2D_81 = 139;
private static readonly var arg_Vd_arrangement_immh_Q___SEEAdvancedSIMDmodifiedimmediate_00__2S_40__4S_41__2D_81 = 140;
private static readonly var arg_Vd_arrangement_immh_Q___SEEAdvancedSIMDmodifiedimmediate_00__8B_10__16B_11__4H_20__8H_21__2S_40__4S_41 = 141;
private static readonly var arg_Vd_arrangement_immh_Q___SEEAdvancedSIMDmodifiedimmediate_00__8B_10__16B_11__4H_20__8H_21__2S_40__4S_41__2D_81 = 142;
private static readonly var arg_Vd_arrangement_immh___SEEAdvancedSIMDmodifiedimmediate_0__8H_1__4S_2__2D_4 = 143;
private static readonly var arg_Vd_arrangement_Q___2S_0__4S_1 = 144;
private static readonly var arg_Vd_arrangement_Q___4H_0__8H_1 = 145;
private static readonly var arg_Vd_arrangement_Q___8B_0__16B_1 = 146;
private static readonly var arg_Vd_arrangement_Q_sz___2S_00__4S_10__2D_11 = 147;
private static readonly var arg_Vd_arrangement_size___4S_1__2D_2 = 148;
private static readonly var arg_Vd_arrangement_size___8H_0__1Q_3 = 149;
private static readonly var arg_Vd_arrangement_size___8H_0__4S_1__2D_2 = 150;
private static readonly var arg_Vd_arrangement_size_Q___4H_00__8H_01__2S_10__4S_11__1D_20__2D_21 = 151;
private static readonly var arg_Vd_arrangement_size_Q___4H_10__8H_11__2S_20__4S_21 = 152;
private static readonly var arg_Vd_arrangement_size_Q___8B_00__16B_01 = 153;
private static readonly var arg_Vd_arrangement_size_Q___8B_00__16B_01__4H_10__8H_11 = 154;
private static readonly var arg_Vd_arrangement_size_Q___8B_00__16B_01__4H_10__8H_11__2S_20__4S_21 = 155;
private static readonly var arg_Vd_arrangement_size_Q___8B_00__16B_01__4H_10__8H_11__2S_20__4S_21__2D_31 = 156;
private static readonly var arg_Vd_arrangement_sz___4S_0__2D_1 = 157;
private static readonly var arg_Vd_arrangement_sz_Q___2S_00__4S_01 = 158;
private static readonly var arg_Vd_arrangement_sz_Q___2S_00__4S_01__2D_11 = 159;
private static readonly var arg_Vd_arrangement_sz_Q___2S_10__4S_11 = 160;
private static readonly var arg_Vd_arrangement_sz_Q___4H_00__8H_01__2S_10__4S_11 = 161;
private static readonly var arg_Vm_22_1__S_0__D_1 = 162;
private static readonly var arg_Vm_22_2__B_0__H_1__S_2__D_3 = 163;
private static readonly var arg_Vm_22_2__D_3 = 164;
private static readonly var arg_Vm_22_2__H_1__S_2 = 165;
private static readonly var arg_Vm_arrangement_4S = 166;
private static readonly var arg_Vm_arrangement_Q___8B_0__16B_1 = 167;
private static readonly var arg_Vm_arrangement_size___8H_0__4S_1__2D_2 = 168;
private static readonly var arg_Vm_arrangement_size___H_1__S_2_index__size_L_H_M__HLM_1__HL_2_1 = 169;
private static readonly var arg_Vm_arrangement_size_Q___4H_10__8H_11__2S_20__4S_21 = 170;
private static readonly var arg_Vm_arrangement_size_Q___8B_00__16B_01 = 171;
private static readonly var arg_Vm_arrangement_size_Q___8B_00__16B_01__1D_30__2D_31 = 172;
private static readonly var arg_Vm_arrangement_size_Q___8B_00__16B_01__4H_10__8H_11__2S_20__4S_21 = 173;
private static readonly var arg_Vm_arrangement_size_Q___8B_00__16B_01__4H_10__8H_11__2S_20__4S_21__2D_31 = 174;
private static readonly var arg_Vm_arrangement_sz_Q___2S_00__4S_01__2D_11 = 175;
private static readonly var arg_Vm_arrangement_sz___S_0__D_1_index__sz_L_H__HL_00__H_10_1 = 176;
private static readonly var arg_Vn_19_4__B_1__H_2__S_4__D_8 = 177;
private static readonly var arg_Vn_19_4__D_8 = 178;
private static readonly var arg_Vn_19_4__H_1__S_2__D_4 = 179;
private static readonly var arg_Vn_19_4__S_4__D_8 = 180;
private static readonly var arg_Vn_1_arrangement_16B = 181;
private static readonly var arg_Vn_22_1__D_1 = 182;
private static readonly var arg_Vn_22_1__S_0__D_1 = 183;
private static readonly var arg_Vn_22_2__B_0__H_1__S_2__D_3 = 184;
private static readonly var arg_Vn_22_2__D_3 = 185;
private static readonly var arg_Vn_22_2__H_0__S_1__D_2 = 186;
private static readonly var arg_Vn_22_2__H_1__S_2 = 187;
private static readonly var arg_Vn_2_arrangement_16B = 188;
private static readonly var arg_Vn_3_arrangement_16B = 189;
private static readonly var arg_Vn_4_arrangement_16B = 190;
private static readonly var arg_Vn_arrangement_16B = 191;
private static readonly var arg_Vn_arrangement_4S = 192;
private static readonly var arg_Vn_arrangement_D_index__1 = 193;
private static readonly var arg_Vn_arrangement_D_index__imm5_1 = 194;
private static readonly var arg_Vn_arrangement_imm5___B_1__H_2_index__imm5__imm5lt41gt_1__imm5lt42gt_2_1 = 195;
private static readonly var arg_Vn_arrangement_imm5___B_1__H_2__S_4__D_8_index__imm5_imm4__imm4lt30gt_1__imm4lt31gt_2__imm4lt32gt_4__imm4lt3gt_8_1 = 196;
private static readonly var arg_Vn_arrangement_imm5___B_1__H_2__S_4__D_8_index__imm5__imm5lt41gt_1__imm5lt42gt_2__imm5lt43gt_4__imm5lt4gt_8_1 = 197;
private static readonly var arg_Vn_arrangement_imm5___B_1__H_2__S_4_index__imm5__imm5lt41gt_1__imm5lt42gt_2__imm5lt43gt_4_1 = 198;
private static readonly var arg_Vn_arrangement_imm5___D_8_index__imm5_1 = 199;
private static readonly var arg_Vn_arrangement_immh_Q___SEEAdvancedSIMDmodifiedimmediate_00__2S_40__4S_41__2D_81 = 200;
private static readonly var arg_Vn_arrangement_immh_Q___SEEAdvancedSIMDmodifiedimmediate_00__8B_10__16B_11__4H_20__8H_21__2S_40__4S_41 = 201;
private static readonly var arg_Vn_arrangement_immh_Q___SEEAdvancedSIMDmodifiedimmediate_00__8B_10__16B_11__4H_20__8H_21__2S_40__4S_41__2D_81 = 202;
private static readonly var arg_Vn_arrangement_immh___SEEAdvancedSIMDmodifiedimmediate_0__8H_1__4S_2__2D_4 = 203;
private static readonly var arg_Vn_arrangement_Q___8B_0__16B_1 = 204;
private static readonly var arg_Vn_arrangement_Q_sz___2S_00__4S_10__2D_11 = 205;
private static readonly var arg_Vn_arrangement_Q_sz___4S_10 = 206;
private static readonly var arg_Vn_arrangement_S_index__imm5__imm5lt41gt_1__imm5lt42gt_2__imm5lt43gt_4_1 = 207;
private static readonly var arg_Vn_arrangement_size___2D_3 = 208;
private static readonly var arg_Vn_arrangement_size___8H_0__4S_1__2D_2 = 209;
private static readonly var arg_Vn_arrangement_size_Q___4H_10__8H_11__2S_20__4S_21 = 210;
private static readonly var arg_Vn_arrangement_size_Q___8B_00__16B_01 = 211;
private static readonly var arg_Vn_arrangement_size_Q___8B_00__16B_01__1D_30__2D_31 = 212;
private static readonly var arg_Vn_arrangement_size_Q___8B_00__16B_01__4H_10__8H_11 = 213;
private static readonly var arg_Vn_arrangement_size_Q___8B_00__16B_01__4H_10__8H_11__2S_20__4S_21 = 214;
private static readonly var arg_Vn_arrangement_size_Q___8B_00__16B_01__4H_10__8H_11__2S_20__4S_21__2D_31 = 215;
private static readonly var arg_Vn_arrangement_size_Q___8B_00__16B_01__4H_10__8H_11__4S_21 = 216;
private static readonly var arg_Vn_arrangement_sz___2D_1 = 217;
private static readonly var arg_Vn_arrangement_sz___2S_0__2D_1 = 218;
private static readonly var arg_Vn_arrangement_sz___4S_0__2D_1 = 219;
private static readonly var arg_Vn_arrangement_sz_Q___2S_00__4S_01 = 220;
private static readonly var arg_Vn_arrangement_sz_Q___2S_00__4S_01__2D_11 = 221;
private static readonly var arg_Vn_arrangement_sz_Q___4H_00__8H_01__2S_10__4S_11 = 222;
private static readonly var arg_Vt_1_arrangement_B_index__Q_S_size_1 = 223;
private static readonly var arg_Vt_1_arrangement_D_index__Q_1 = 224;
private static readonly var arg_Vt_1_arrangement_H_index__Q_S_size_1 = 225;
private static readonly var arg_Vt_1_arrangement_S_index__Q_S_1 = 226;
private static readonly var arg_Vt_1_arrangement_size_Q___8B_00__16B_01__4H_10__8H_11__2S_20__4S_21__1D_30__2D_31 = 227;
private static readonly var arg_Vt_2_arrangement_B_index__Q_S_size_1 = 228;
private static readonly var arg_Vt_2_arrangement_D_index__Q_1 = 229;
private static readonly var arg_Vt_2_arrangement_H_index__Q_S_size_1 = 230;
private static readonly var arg_Vt_2_arrangement_S_index__Q_S_1 = 231;
private static readonly var arg_Vt_2_arrangement_size_Q___8B_00__16B_01__4H_10__8H_11__2S_20__4S_21__1D_30__2D_31 = 232;
private static readonly var arg_Vt_2_arrangement_size_Q___8B_00__16B_01__4H_10__8H_11__2S_20__4S_21__2D_31 = 233;
private static readonly var arg_Vt_3_arrangement_B_index__Q_S_size_1 = 234;
private static readonly var arg_Vt_3_arrangement_D_index__Q_1 = 235;
private static readonly var arg_Vt_3_arrangement_H_index__Q_S_size_1 = 236;
private static readonly var arg_Vt_3_arrangement_S_index__Q_S_1 = 237;
private static readonly var arg_Vt_3_arrangement_size_Q___8B_00__16B_01__4H_10__8H_11__2S_20__4S_21__1D_30__2D_31 = 238;
private static readonly var arg_Vt_3_arrangement_size_Q___8B_00__16B_01__4H_10__8H_11__2S_20__4S_21__2D_31 = 239;
private static readonly var arg_Vt_4_arrangement_B_index__Q_S_size_1 = 240;
private static readonly var arg_Vt_4_arrangement_D_index__Q_1 = 241;
private static readonly var arg_Vt_4_arrangement_H_index__Q_S_size_1 = 242;
private static readonly var arg_Vt_4_arrangement_S_index__Q_S_1 = 243;
private static readonly var arg_Vt_4_arrangement_size_Q___8B_00__16B_01__4H_10__8H_11__2S_20__4S_21__1D_30__2D_31 = 244;
private static readonly var arg_Vt_4_arrangement_size_Q___8B_00__16B_01__4H_10__8H_11__2S_20__4S_21__2D_31 = 245;
private static readonly var arg_Wa = 246;
private static readonly var arg_Wd = 247;
private static readonly var arg_Wds = 248;
private static readonly var arg_Wm = 249;
private static readonly var arg_Wm_extend__UXTB_0__UXTH_1__LSL_UXTW_2__UXTX_3__SXTB_4__SXTH_5__SXTW_6__SXTX_7__0_4 = 250;
private static readonly var arg_Wm_shift__LSL_0__LSR_1__ASR_2__0_31 = 251;
private static readonly var arg_Wm_shift__LSL_0__LSR_1__ASR_2__ROR_3__0_31 = 252;
private static readonly var arg_Wn = 253;
private static readonly var arg_Wns = 254;
private static readonly var arg_Ws = 255;
private static readonly var arg_Wt = 256;
private static readonly var arg_Wt2 = 257;
private static readonly var arg_Xa = 258;
private static readonly var arg_Xd = 259;
private static readonly var arg_Xds = 260;
private static readonly var arg_Xm = 261;
private static readonly var arg_Xm_shift__LSL_0__LSR_1__ASR_2__0_63 = 262;
private static readonly var arg_Xm_shift__LSL_0__LSR_1__ASR_2__ROR_3__0_63 = 263;
private static readonly var arg_Xn = 264;
private static readonly var arg_Xns = 265;
private static readonly var arg_Xns_mem = 266;
private static readonly var arg_Xns_mem_extend_m__UXTW_2__LSL_3__SXTW_6__SXTX_7__0_0__1_1 = 267;
private static readonly var arg_Xns_mem_extend_m__UXTW_2__LSL_3__SXTW_6__SXTX_7__0_0__2_1 = 268;
private static readonly var arg_Xns_mem_extend_m__UXTW_2__LSL_3__SXTW_6__SXTX_7__0_0__3_1 = 269;
private static readonly var arg_Xns_mem_extend_m__UXTW_2__LSL_3__SXTW_6__SXTX_7__0_0__4_1 = 270;
private static readonly var arg_Xns_mem_extend_m__UXTW_2__LSL_3__SXTW_6__SXTX_7__absent_0__0_1 = 271;
private static readonly var arg_Xns_mem_offset = 272;
private static readonly var arg_Xns_mem_optional_imm12_16_unsigned = 273;
private static readonly var arg_Xns_mem_optional_imm12_1_unsigned = 274;
private static readonly var arg_Xns_mem_optional_imm12_2_unsigned = 275;
private static readonly var arg_Xns_mem_optional_imm12_4_unsigned = 276;
private static readonly var arg_Xns_mem_optional_imm12_8_unsigned = 277;
private static readonly var arg_Xns_mem_optional_imm7_16_signed = 278;
private static readonly var arg_Xns_mem_optional_imm7_4_signed = 279;
private static readonly var arg_Xns_mem_optional_imm7_8_signed = 280;
private static readonly var arg_Xns_mem_optional_imm9_1_signed = 281;
private static readonly var arg_Xns_mem_post_fixedimm_1 = 282;
private static readonly var arg_Xns_mem_post_fixedimm_12 = 283;
private static readonly var arg_Xns_mem_post_fixedimm_16 = 284;
private static readonly var arg_Xns_mem_post_fixedimm_2 = 285;
private static readonly var arg_Xns_mem_post_fixedimm_24 = 286;
private static readonly var arg_Xns_mem_post_fixedimm_3 = 287;
private static readonly var arg_Xns_mem_post_fixedimm_32 = 288;
private static readonly var arg_Xns_mem_post_fixedimm_4 = 289;
private static readonly var arg_Xns_mem_post_fixedimm_6 = 290;
private static readonly var arg_Xns_mem_post_fixedimm_8 = 291;
private static readonly var arg_Xns_mem_post_imm7_16_signed = 292;
private static readonly var arg_Xns_mem_post_imm7_4_signed = 293;
private static readonly var arg_Xns_mem_post_imm7_8_signed = 294;
private static readonly var arg_Xns_mem_post_imm9_1_signed = 295;
private static readonly var arg_Xns_mem_post_Q__16_0__32_1 = 296;
private static readonly var arg_Xns_mem_post_Q__24_0__48_1 = 297;
private static readonly var arg_Xns_mem_post_Q__32_0__64_1 = 298;
private static readonly var arg_Xns_mem_post_Q__8_0__16_1 = 299;
private static readonly var arg_Xns_mem_post_size__1_0__2_1__4_2__8_3 = 300;
private static readonly var arg_Xns_mem_post_size__2_0__4_1__8_2__16_3 = 301;
private static readonly var arg_Xns_mem_post_size__3_0__6_1__12_2__24_3 = 302;
private static readonly var arg_Xns_mem_post_size__4_0__8_1__16_2__32_3 = 303;
private static readonly var arg_Xns_mem_post_Xm = 304;
private static readonly var arg_Xns_mem_wb_imm7_16_signed = 305;
private static readonly var arg_Xns_mem_wb_imm7_4_signed = 306;
private static readonly var arg_Xns_mem_wb_imm7_8_signed = 307;
private static readonly var arg_Xns_mem_wb_imm9_1_signed = 308;
private static readonly var arg_Xs = 309;
private static readonly var arg_Xt = 310;
private static readonly var arg_Xt2 = 311;

} // end arm64asm_package
