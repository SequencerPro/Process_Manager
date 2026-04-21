#!/usr/bin/env bash
# Demonstrates Phase 23 BOM-aware Process validation by building a small robot.
#
# BOMs:
#   Robot   = 1 Chassis + 2 Arm + 4 Screw
#   Chassis = 1 Base + 4 Wheel + 2 Motor + 8 Screw
#   Arm     = 3 Servo + 1 Gripper + 6 Screw
#
# Each assembly process has 3 steps with work-instruction content blocks.
# Inputs are split across steps; the BOM validator sums them per ComponentKind.
set -euo pipefail

API=http://localhost:5183
PFX="${PFX:-RBT}"

say() { printf '\n\033[1;36m== %s ==\033[0m\n' "$1"; }

TOKEN=$(curl -s -X POST "$API/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin","password":"Admin1234!"}' | /c/Windows/py -3 -c 'import sys,json;print(json.load(sys.stdin)["token"])')
AUTH=(-H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json')

post() { curl -s "${AUTH[@]}" -X POST "$API$1" -d "$2"; }
jqid() { /c/Windows/py -3 -c 'import sys,json;print(json.load(sys.stdin)["id"])'; }
jstr() { /c/Windows/py -3 -c 'import json,sys;print(json.dumps(sys.argv[1]))' "$1"; }

mkkind() {
  local code="$1" name="$2" source="$3"
  post /api/kinds "$(printf '{"code":"%s","name":"%s","description":null,"isSerialized":false,"isBatchable":false,"sourceType":"%s"}' "$code" "$name" "$source")" | jqid
}
mkgrade() {
  post "/api/kinds/$1/grades" '{"code":"STD","name":"Standard","description":null,"isDefault":true,"sortOrder":0}' | jqid
}
mkbom() {
  post "/api/kinds/$1/bom" "$(printf '{"componentKindId":"%s","lineNumber":%d,"quantity":%s,"sortOrder":0}' "$2" "$3" "$4")" > /dev/null
}

port_in()  { printf '{"name":"%s","direction":"Input","portType":"Material","kindId":"%s","gradeId":"%s","qtyRuleMode":"Exactly","qtyRuleN":%d,"sortOrder":%d}' "$1" "$2" "$3" "$4" "$5"; }
port_out() { printf '{"name":"%s","direction":"Output","portType":"Material","kindId":"%s","gradeId":"%s","qtyRuleMode":"Exactly","qtyRuleN":1,"sortOrder":%d}' "$1" "$2" "$3" "$4"; }

# Create a StepTemplate (General pattern — no structural constraints).
# Usage: mktmpl <code> <name> <desc> <ports-json-list>
mktmpl() {
  local code="$1" name="$2" desc="$3" ports="$4"
  post /api/steptemplates "$(printf '{"code":"%s","name":"%s","description":"%s","pattern":"General","isShared":true,"ports":[%s]}' \
    "$code" "$name" "$desc" "$ports")" | jqid
}

# Attach a text work-instruction content block. Category = Setup|Safety|Inspection|Reference|Note.
addtext() {
  local tmpl="$1" body="$2" category="${3:-Setup}"
  post "/api/steptemplates/$tmpl/content/text" \
    "$(printf '{"body":%s,"contentCategory":"%s"}' "$(jstr "$body")" "$category")" > /dev/null
}

mkproc() {
  post /api/processes "$(printf '{"code":"%s","name":"%s","description":null,"processRole":"ManufacturingProcess"}' "$1" "$2")" | jqid
}
addstep() {
  post "/api/processes/$1/steps" "$(printf '{"stepTemplateId":"%s","sequence":%d}' "$2" "$3")" > /dev/null
}

say "1. Creating Kinds"
BASE_K=$(mkkind  "$PFX-BASE"  "Robot Base" Buy) ; BASE_G=$(mkgrade  "$BASE_K")
WHEEL_K=$(mkkind "$PFX-WHEEL" "Wheel"      Buy) ; WHEEL_G=$(mkgrade "$WHEEL_K")
MOTOR_K=$(mkkind "$PFX-MOTOR" "Motor"      Buy) ; MOTOR_G=$(mkgrade "$MOTOR_K")
SCREW_K=$(mkkind "$PFX-SCREW" "M3 Screw"   Buy) ; SCREW_G=$(mkgrade "$SCREW_K")
SERVO_K=$(mkkind "$PFX-SERVO" "Servo"      Buy) ; SERVO_G=$(mkgrade "$SERVO_K")
GRIP_K=$(mkkind  "$PFX-GRIP"  "Gripper"    Buy) ; GRIP_G=$(mkgrade  "$GRIP_K")

CHASSIS_K=$(mkkind "$PFX-CHASS" "Chassis Assembly" Make) ; CHASSIS_G=$(mkgrade "$CHASSIS_K")
ARM_K=$(mkkind     "$PFX-ARM"   "Arm Assembly"     Make) ; ARM_G=$(mkgrade     "$ARM_K")
ROBOT_K=$(mkkind   "$PFX-ROBOT" "Complete Robot"   Make) ; ROBOT_G=$(mkgrade   "$ROBOT_K")
echo "  Buy leaves: Base Wheel Motor Screw Servo Gripper"
echo "  Make assemblies: Chassis, Arm, Robot"

say "2. Creating BOMs"
mkbom "$CHASSIS_K" "$BASE_K"  1 1
mkbom "$CHASSIS_K" "$WHEEL_K" 2 4
mkbom "$CHASSIS_K" "$MOTOR_K" 3 2
mkbom "$CHASSIS_K" "$SCREW_K" 4 8
echo "  Chassis: 1 Base + 4 Wheel + 2 Motor + 8 Screw"

mkbom "$ARM_K" "$SERVO_K" 1 3
mkbom "$ARM_K" "$GRIP_K"  2 1
mkbom "$ARM_K" "$SCREW_K" 3 6
echo "  Arm:     3 Servo + 1 Gripper + 6 Screw"

mkbom "$ROBOT_K" "$CHASSIS_K" 1 1
mkbom "$ROBOT_K" "$ARM_K"     2 2
mkbom "$ROBOT_K" "$SCREW_K"   3 4
echo "  Robot:   1 Chassis + 2 Arm + 4 Screw"

# ════════════ CHASSIS process (3 steps) ════════════
say "3. Chassis process (3 steps, with work instructions)"

CH_T1=$(mktmpl "$PFX-T-CH1" "Prep Base" "Inspect base and stage fasteners" \
  "$(port_in "Base In" "$BASE_K" "$BASE_G" 1 0),$(port_in "Mount Screws" "$SCREW_K" "$SCREW_G" 4 1)")
addtext "$CH_T1" "SAFETY: Workbench must be clear; ESD mat grounded before handling the base." Safety
addtext "$CH_T1" "1. Remove the robot base from its wrap and confirm the part number matches the traveler.\n2. Set the base on the fixture with mounting holes facing up.\n3. Count out 4 M3 screws into the parts tray (for wheel-motor mounts in step 2)." Setup
addtext "$CH_T1" "Visually inspect the base for cracks, burrs, or plating defects. Reject if any are found." Inspection

CH_T2=$(mktmpl "$PFX-T-CH2" "Install Drive Train" "Attach wheels and motors to the base" \
  "$(port_in "Wheels In" "$WHEEL_K" "$WHEEL_G" 4 0),$(port_in "Motors In" "$MOTOR_K" "$MOTOR_G" 2 1),$(port_in "Drive Screws" "$SCREW_K" "$SCREW_G" 2 2)")
addtext "$CH_T2" "1. Press one wheel onto each motor shaft until the retaining clip seats (audible click).\n2. Align each motor with its mounting boss on the base.\n3. Drive 1 screw per motor to 0.6 Nm using the torque driver." Setup
addtext "$CH_T2" "Fit the remaining 2 idler wheels to the front axle pins." Setup
addtext "$CH_T2" "Verify wheel rotation is free and motor leads are routed through the strain-relief channel." Inspection

CH_T3=$(mktmpl "$PFX-T-CH3" "Close Chassis" "Torque remaining fasteners and issue finished chassis" \
  "$(port_in "Final Screws" "$SCREW_K" "$SCREW_G" 2 0),$(port_out "Chassis Out" "$CHASSIS_K" "$CHASSIS_G" 1)")
addtext "$CH_T3" "1. Drive the 2 remaining M3 screws to close the cover plate.\n2. Torque all fasteners to 0.6 Nm in a cross pattern.\n3. Tag the assembly with its Chassis serial number." Setup
addtext "$CH_T3" "Confirm all 8 screws are present and fully torqued before releasing the chassis." Inspection
addtext "$CH_T3" "Route the finished chassis to the Arm-mating station." Reference

CHASSIS_PROC=$(mkproc "$PFX-P-CHASS" "Chassis Assembly Process")
addstep "$CHASSIS_PROC" "$CH_T1" 1
addstep "$CHASSIS_PROC" "$CH_T2" 2
addstep "$CHASSIS_PROC" "$CH_T3" 3
echo "  Steps: Prep Base -> Install Drive Train -> Close Chassis"

# ════════════ ARM process (3 steps) ════════════
say "4. Arm process (3 steps, with work instructions)"

AR_T1=$(mktmpl "$PFX-T-AR1" "Mount Servos" "Fasten 3 servos to the linkage plate" \
  "$(port_in "Servos In" "$SERVO_K" "$SERVO_G" 3 0),$(port_in "Servo Screws" "$SCREW_K" "$SCREW_G" 3 1)")
addtext "$AR_T1" "SAFETY: Disconnect servo leads before handling to avoid unexpected jog on power-up." Safety
addtext "$AR_T1" "1. Lay the linkage plate on the arm fixture.\n2. Seat each of 3 servos into its pocket with the spline facing outboard.\n3. Drive 1 screw per servo to 0.4 Nm." Setup
addtext "$AR_T1" "Verify each servo is square to the plate (use the 90-degree gauge)." Inspection

AR_T2=$(mktmpl "$PFX-T-AR2" "Attach Gripper" "Install and align the gripper assembly" \
  "$(port_in "Gripper In" "$GRIP_K" "$GRIP_G" 1 0),$(port_in "Gripper Screws" "$SCREW_K" "$SCREW_G" 2 1)")
addtext "$AR_T2" "1. Slide the gripper onto the wrist servo spline.\n2. Align the index mark with the arrow on the servo housing.\n3. Drive 2 screws to 0.4 Nm through the retaining collar." Setup
addtext "$AR_T2" "Open and close the gripper by hand and confirm smooth travel over full range." Inspection

AR_T3=$(mktmpl "$PFX-T-AR3" "Calibrate & Label" "Final torque check and serial-number tag" \
  "$(port_in "Cover Screws" "$SCREW_K" "$SCREW_G" 1 0),$(port_out "Arm Out" "$ARM_K" "$ARM_G" 1)")
addtext "$AR_T3" "1. Install the cable-cover screw.\n2. Connect the calibration fixture and run routine CAL-ARM-01.\n3. Apply the serialized Arm label to the elbow housing." Setup
addtext "$AR_T3" "Calibration report must show all three joints within +/- 1 degree before sign-off." Inspection

ARM_PROC=$(mkproc "$PFX-P-ARM" "Arm Assembly Process")
addstep "$ARM_PROC" "$AR_T1" 1
addstep "$ARM_PROC" "$AR_T2" 2
addstep "$ARM_PROC" "$AR_T3" 3
echo "  Steps: Mount Servos -> Attach Gripper -> Calibrate & Label"

# ════════════ ROBOT process (3 steps) ════════════
say "5. Robot process (3 steps, with work instructions)"

RB_T1=$(mktmpl "$PFX-T-RB1" "Mount Left Arm" "Bolt the left arm to the chassis shoulder" \
  "$(port_in "Left Arm In" "$ARM_K" "$ARM_G" 1 0),$(port_in "Left Mount Screws" "$SCREW_K" "$SCREW_G" 2 1)")
addtext "$RB_T1" "SAFETY: Power bus must be disconnected during arm mating." Safety
addtext "$RB_T1" "1. Present the left arm to the port-side shoulder boss.\n2. Align the keying pin with the locating hole.\n3. Drive 2 M3 screws to 0.8 Nm." Setup
addtext "$RB_T1" "Connect and strain-relieve the left arm harness." Setup

RB_T2=$(mktmpl "$PFX-T-RB2" "Mount Right Arm" "Bolt the right arm to the chassis shoulder" \
  "$(port_in "Right Arm In" "$ARM_K" "$ARM_G" 1 0),$(port_in "Right Mount Screws" "$SCREW_K" "$SCREW_G" 2 1)")
addtext "$RB_T2" "1. Present the right arm to the starboard shoulder boss.\n2. Align the keying pin with the locating hole.\n3. Drive 2 M3 screws to 0.8 Nm." Setup
addtext "$RB_T2" "Connect and strain-relieve the right arm harness; confirm polarity on J4." Inspection

RB_T3=$(mktmpl "$PFX-T-RB3" "Mate Chassis & Commission" "Seat the chassis and issue finished robot" \
  "$(port_in "Chassis In" "$CHASSIS_K" "$CHASSIS_G" 1 0),$(port_out "Robot Out" "$ROBOT_K" "$ROBOT_G" 1)")
addtext "$RB_T3" "1. Seat the arm-body assembly onto the chassis upper plate.\n2. Close the 4 quarter-turn latches.\n3. Plug the main power bus into J1 and run self-test POST-ROBOT-01." Setup
addtext "$RB_T3" "All four arm joints and both drive wheels must respond to the commissioning test before the robot is released to QA." Inspection
addtext "$RB_T3" "Apply the finished Robot serial-number plate to the rear access hatch." Reference

ROBOT_PROC=$(mkproc "$PFX-P-ROBOT" "Robot Assembly Process")
addstep "$ROBOT_PROC" "$RB_T1" 1
addstep "$ROBOT_PROC" "$RB_T2" 2
addstep "$ROBOT_PROC" "$RB_T3" 3
echo "  Steps: Mount Left Arm -> Mount Right Arm -> Mate Chassis & Commission"

# ════════════ BROKEN ROBOT process (3 steps; deliberately wrong) ════════════
say "6. Broken Robot process (3 steps; engineer miscounted)"

BR_T1=$(mktmpl "$PFX-T-RBB1" "Mount Arm (bad)" "Operator installs only one arm" \
  "$(port_in "Arm In" "$ARM_K" "$ARM_G" 1 0)")
addtext "$BR_T1" "(Draft) Bolt the arm onto the chassis. Note: engineer has not yet decided which side." Note

BR_T2=$(mktmpl "$PFX-T-RBB2" "Seat Chassis (bad)" "Place chassis on jig" \
  "$(port_in "Chassis In" "$CHASSIS_K" "$CHASSIS_G" 1 0)")
addtext "$BR_T2" "(Draft) Place the chassis on the final-assembly jig and lock the quarter-turns." Setup

BR_T3=$(mktmpl "$PFX-T-RBB3" "Finalize (bad)" "Emit finished robot (screws forgotten)" \
  "$(port_out "Robot Out" "$ROBOT_K" "$ROBOT_G" 0)")
addtext "$BR_T3" "(Draft) Close up and ship. TODO: revisit fastener list — pretty sure we need more bolts." Note

BAD_PROC=$(mkproc "$PFX-P-ROBOT-BAD" "Robot Assembly (broken draft)")
addstep "$BAD_PROC" "$BR_T1" 1
addstep "$BAD_PROC" "$BR_T2" 2
addstep "$BAD_PROC" "$BR_T3" 3
echo "  Steps: Mount Arm (only 1) -> Seat Chassis -> Finalize (no screws)"

# ════════════ VALIDATE ════════════
say "7. Validating"
validate() {
  echo
  echo "-- $1 --"
  curl -s "${AUTH[@]}" "$API/api/processes/$2/validate" | /c/Windows/py -3 -c '
import sys, json
r = json.load(sys.stdin)
print("  errors:   ", len(r["errors"]))
for e in r["errors"]:   print("    ERROR   ", e)
print("  warnings: ", len(r["warnings"]))
for w in r["warnings"]: print("    WARNING ", w)
'
}

validate "Chassis process (should pass — 8 screws across 3 steps)" "$CHASSIS_PROC"
validate "Arm process (should pass — 6 screws across 3 steps)"    "$ARM_PROC"
validate "Robot process (should pass — 4 screws across 3 steps)"  "$ROBOT_PROC"
validate "Broken Robot process (should fail — 1 arm, 0 screws)"   "$BAD_PROC"

echo
echo "Prefix used: $PFX"
echo "Processes:"
echo "  Chassis     $CHASSIS_PROC"
echo "  Arm         $ARM_PROC"
echo "  Robot       $ROBOT_PROC"
echo "  Robot-BAD   $BAD_PROC"
