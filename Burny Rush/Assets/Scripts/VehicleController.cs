using System.Collections;
using UnityEngine;
using TMPro;

public enum GearState
{
    Neutral,
    Running,
    CheckingChange,
    Changing
};

public class VehicleController : MonoBehaviour
{
    private float horizontalInput, verticalInput, brakeInput;
    private float currentSteerAngle, currentbreakForce;
    private float steeringWheelZ;
    private float clutch;
    private float engineRPM;
    private float wheelRPM;
    private float currentTorque;
    private float carMovementDirection;

    private bool isBreaking;
    private bool isEngineBraking;

    private int currentGear;
    private GearState gearState;

    [HideInInspector]
    public float velocity;
    public float speed;

    private Rigidbody vehicleRigidbody;

    [Header("Engine & Powertrain")]
    
    [SerializeField] private float reverse;
    [SerializeField] private float horsePower;
    [SerializeField] private float brakeForce;
    [SerializeField] private float engineFriction;
    [Range(0f, 1f)]
    [SerializeField] private float engineCompression;
    [Range(0f, 10000f)]
    [SerializeField] private float idleRPM, maxRPM;
    [SerializeField] private float[] gearRatios;
    [Range(0f, 10f)]
    [SerializeField] private float differentialRatio;
    [SerializeField] private float changeGearTime;
    [SerializeField] private float increaseGearRPM;
    [SerializeField] private float decreaseGearRPM;
    [SerializeField] private Speedometer speedometer;
    [SerializeField] private AnimationCurve horsePowerCurve;

    [Range(0f, 1f)]
    [SerializeField] private float frontWheelPowerRatio, rearWheelPowerRatio;
    public bool startEngine;
    [Space(10)]

    [Header("Aero & Physics")]

    [SerializeField] private Transform centerOfMass;
    [SerializeField] private float antiRollFactor;
    [SerializeField] private float aeroDrag;
    [SerializeField] private float maxSteerAngle;
    [Space(10)]

    [Header("Others")]

    [SerializeField] private WheelCollider frontLeftWheelCollider; 
    [SerializeField] private WheelCollider frontRightWheelCollider;
    [SerializeField] private WheelCollider rearLeftWheelCollider; 
    [SerializeField] private WheelCollider rearRightWheelCollider;

    [SerializeField] private Transform steeringWheel;
    [SerializeField] private Transform frontLeftWheelTransform, frontRightWheelTransform;
    [SerializeField] private Transform rearLeftWheelTransform, rearRightWheelTransform;

    private void Start() 
    {
        vehicleRigidbody = GetComponent<Rigidbody>();
        vehicleRigidbody.centerOfMass = centerOfMass.localPosition;

        horsePower = horsePower * 3f;
        EngineStart();
    }

    private void FixedUpdate() 
    {
        ParameterCalculation();
        UpdateWheels();
        Aerodynamics();

        if(startEngine)
        {
            GetInput();
            AntiRoll();
            GearShift();
            HandleMotor();
            HandleSteering();
        }
    }

    private void ParameterCalculation()
    {
        velocity = Vector3.Dot(vehicleRigidbody.velocity, transform.forward) * 4.24f;
        speed = Mathf.Abs(velocity);

        if(speedometer)
        {
            speedometer.rpm = engineRPM;
            speedometer.speed = Mathf.RoundToInt(speed);
            speedometer.current_gear = (velocity < -2 && currentGear == 0) ? "R" : (currentGear + 1).ToString();
        }

        if(velocity > 0)
        {
            carMovementDirection = 1;
        }
        else if(velocity < 0)
        {
            carMovementDirection = -1;
        }
        else
        {
            carMovementDirection = 0;
        }
    }

    private void GetInput() 
    {
        if(carMovementDirection == 1 && verticalInput < 0)
        {
            isBreaking = true;
        }
        else if(carMovementDirection == -1 && verticalInput > 0)
        {
            isBreaking = true;
        }
        else
        {
            isBreaking = false;
        }
    }

    private void Aerodynamics()
    {
        vehicleRigidbody.drag = 0; 
        vehicleRigidbody.angularDrag = 0.05f + (Mathf.Pow(speed, 1.5f) * aeroDrag / 1000000);
    }

    private void GearShift()
    {
        if(engineRPM > increaseGearRPM && currentGear < gearRatios.Length - 1)
        {
            currentGear += 1;
        }
        if(engineRPM < decreaseGearRPM && currentGear > 0)
        {
            currentGear -= 1;
        }
    }

    private float CalculatingTorque()
    {
        float torque = 0;
        if(startEngine)
        {
            if(clutch < 0.1f)
            {
                engineRPM = Mathf.Lerp(engineRPM, Mathf.Max(idleRPM, maxRPM * verticalInput), Time.deltaTime);
            }
            else
            {
                wheelRPM = Mathf.Lerp(wheelRPM, Mathf.Abs((rearLeftWheelCollider.rpm + rearRightWheelCollider.rpm) / 2) * gearRatios[currentGear] * differentialRatio, Time.deltaTime * (1 / engineFriction));
                engineRPM = Mathf.Clamp(Mathf.Max(idleRPM, wheelRPM), 0, maxRPM);
                
                if(velocity > -reverse && engineRPM < maxRPM && !isBreaking)
                {
                    if(verticalInput == 0 && engineRPM > idleRPM + 500f)
                    {
                        isEngineBraking = true;
                        torque = (horsePowerCurve.Evaluate(engineRPM/maxRPM) * horsePower/engineRPM) * gearRatios[currentGear] * differentialRatio * 1000f * clutch * engineCompression;
                    }
                    else
                    {
                        isEngineBraking = false;
                        torque = (horsePowerCurve.Evaluate(engineRPM/maxRPM) * horsePower/engineRPM) * gearRatios[currentGear] * differentialRatio * 1000f * clutch;
                    }
                }
                else
                {
                    isEngineBraking = false;
                    torque = 0;
                }
            }
        }
        return torque;
    }

    private void HandleMotor() 
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        brakeInput = Input.GetAxis("Jump");

        if (Mathf.Abs(verticalInput) > 0 && !startEngine)
        {
            gearState = GearState.Running;
        }

        if (gearState != GearState.Changing)
        {
            if (gearState == GearState.Neutral)
            {
                clutch = 0;
                if (Mathf.Abs( verticalInput )> 0)
                {
                    gearState = GearState.Running;
                }
            }
            else
            {
                clutch = Input.GetKey(KeyCode.LeftShift) ? 0 : Mathf.Lerp(clutch, 1, Time.deltaTime);
            }
        }
        else
        {
            clutch = 0;
        }

        currentTorque = CalculatingTorque();

        float frontTorque = isEngineBraking ? (-carMovementDirection * currentTorque * frontWheelPowerRatio) : (verticalInput * currentTorque * frontWheelPowerRatio);
        float rearTorque = isEngineBraking ? (-carMovementDirection * currentTorque * rearWheelPowerRatio) : (verticalInput * currentTorque * rearWheelPowerRatio * Mathf.Abs(1 - brakeInput));

        frontLeftWheelCollider.motorTorque = frontTorque;
        frontRightWheelCollider.motorTorque = frontTorque;

        rearLeftWheelCollider.motorTorque = rearTorque;
        rearRightWheelCollider.motorTorque = rearTorque;

        currentbreakForce = isBreaking ? brakeForce : 0f;
        ApplyBreaking();
    }

    private void ApplyBreaking() 
    {
        frontRightWheelCollider.brakeTorque = currentbreakForce;
        frontLeftWheelCollider.brakeTorque = currentbreakForce;
        rearLeftWheelCollider.brakeTorque = currentbreakForce + brakeForce * brakeInput;
        rearRightWheelCollider.brakeTorque = currentbreakForce + brakeForce * brakeInput;
    }

    private void HandleSteering() 
    {
        currentSteerAngle = maxSteerAngle * horizontalInput;
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;

        if(steeringWheel)
        {
            steeringWheelZ = Mathf.Lerp(steeringWheelZ, horizontalInput * maxSteerAngle * 2f, 10f * Time.deltaTime);
            steeringWheel.localRotation = Quaternion.Euler(steeringWheel.localRotation.eulerAngles.x, 0f, -steeringWheelZ);
        }
    }

    private void AntiRoll()
    {
        WheelHit hit;
		float travelL = 1.0f;
		float travelR = 1.0f;

		bool groundedL = rearLeftWheelCollider.GetGroundHit (out hit);
		if (groundedL) {
			travelL = (-rearLeftWheelCollider.transform.InverseTransformPoint (hit.point).y - rearLeftWheelCollider.radius) / rearLeftWheelCollider.suspensionDistance;
		}

		bool groundedR = rearRightWheelCollider.GetGroundHit (out hit);
		if (groundedR) {
			travelR = (-rearRightWheelCollider.transform.InverseTransformPoint (hit.point).y - rearRightWheelCollider.radius) / rearRightWheelCollider.suspensionDistance;
		}

		float antiRollForce = (travelL - travelR) * antiRollFactor * Mathf.Pow(speed, 2) / 100f;

		if (groundedL)
			vehicleRigidbody.AddForceAtPosition (rearLeftWheelCollider.transform.up * -antiRollForce, rearLeftWheelCollider.transform.position);

		if (groundedR)
			vehicleRigidbody.AddForceAtPosition (rearRightWheelCollider.transform.up * antiRollForce, rearRightWheelCollider.transform.position);
    }

    private void UpdateWheels() 
    {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform) 
    {
        Vector3 pos;
        Quaternion rot; 
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.rotation = rot;
        wheelTransform.position = pos;
    }

    private void EngineStart()
    {
        engineRPM = 900;

        if(speedometer)
        {
            speedometer.SetRedline(maxRPM);
        }
    }
}