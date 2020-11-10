/* ========================================================================================================
 * 70:30 SmoothOrbitCam Script - created by D.Michalke / 70:30 / http://70-30.de / info@70-30.de
 * used to orbit smoothly around an object! drag and drop on your camera and drag the targed object on the target slot
 * ========================================================================================================
 */

using System;
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.EventSystems;

//add a menu to Component in Unity
[AddComponentMenu("Camera-Control/SmoothOrbitCam")]

public class SmoothOrbitCam : MonoBehaviour
{
	//transform to drag and drop the target object
	public Transform target;

    //useable or not, used for viewchanger
    [HideInInspector]
    public bool useable = true;

    [Space(20)]
    [Header("Orbiting")]
    //enable orbiting. deactivate if you need a pan-only cam (i.e. strategy games etc.)
    public bool EnableOrbiting = true;

    [Tooltip("Choose a key for orbiting. Default is Mouse1.")]
    //the key for orbiting
    public KeyCode orbitKey;

    [Tooltip("Orbiting speed.")]
    //add speed variables for orbiting speed
    public float xSpeed = 10.0f;
    public float ySpeed = 10.0f;

    [Tooltip("The starting distance to the target object.")]
    //add the distance variable for zooming in and out with the mouse wheel
    public float distance = 5.0f;
    private float lerpDistance = 0;

    [Tooltip("The limits for the zoom distance.")]
    //add limits for the zoom distance
    public float distanceMin = 3f;
    public float distanceMax = 15f;

    [Tooltip("Limits the orbiting to the X or Y axis.")]
    //enable or disable axes
    public bool limitToXAxis = false;
	public bool limitToYAxis = false;

    [Tooltip("The limits for the rotation axes.")]
    //add limits for the rotation axes
    public float yMinLimit = -20f;
	public float yMaxLimit = 80f;
	public float xMinLimit = -360;
	public float xMaxLimit = 360;
    private float storedLimit = 0;

    [Tooltip("The amount of smooth-out-effect.")]
    //add the smoothing variable
    public float smoothTime = 2f;

    //define the rotation axes
    float rotationYAxis = 0.0f;
    float rotationXAxis = 0.0f;
    [HideInInspector]
    public Quaternion rotation;

    //define the main velocity
    [HideInInspector]
    public float velocityX = 0.0f;
    [HideInInspector]
    public float velocityY = 0.0f;

    [Space(20)]
    [Header("Zooming")]

    [Tooltip("Zooming works via mouse wheel on desktop and 2-finger-pinch-gesture on mobile.")]
    //zoom
    public bool enableZooming = true;
    //add a modifyer for zooming in and out (for both touch and mouse)
    [Tooltip("The zooming speed.")]
    public float zoomSpeed = 1;

    [Space(20)]
    [Header("Panning")]
    //enable panning - for panning, the camera has to be assigned to target pan cam and the smoothorbitcam.cs script has to be on a parent object with the camera as child object!
    public bool enablePanning = false;
    [HideInInspector]
    public GameObject targetPanCam;

    [Tooltip("Choose a key for panning. Default is Mouse2.")]
    public KeyCode panKey;

    [Tooltip("Panning speed.")]
    public float panSpeed = 1;
    [Tooltip("Limit panning (Add invisible borders).")]
    public bool LimitPan = false;
    [Tooltip("Enter the panning limits in units here if required.")]
    public Vector2 PanLimitsLeftRight;
    public Vector2 PanLimitsUpDown;

    [Space(20)]
    [Header("Miscellaneous")]

    [Tooltip("Offset to the center of the focus point (sometimes useful, if the pivot of a 3D-object is not centered).")]
    //add offset variables to get more control over the cam
    public float xOffset;
	public float yOffset;

    [Tooltip("Enables automatic orbiting.")]
    //automatic orbiting
    public bool enableAutomaticOrbiting = false;
    [Tooltip("Automatic orbiting speed. Use negative values for opposite orbiting direction.")]
    public float orbitingSpeed = 1f;

    [Tooltip("If you want the camera to move in front of objects that are in the way to the target object, set this to true. Default value is false.")]
    //for objects that might be between the target object and the cam
    public bool NoObjectsBetween = false;

    [Tooltip("Keeps the camera above the ground (beta).")]
    //the minimum distance the cam stays away from a possible ground (for RPGs, racing games, etc to keep the cam in a good position)
    public bool EnableGroundHovering = true;
    public float GroundHoverDistance = 5;

    [Tooltip("If you want UI Elements to block the orbiting camera interaction, set this to true.")]
    //if ui should block interaction with orbit cam
    public bool UiBlocksInteraction = false;
	private bool uiBlocking = false;

    [Space(20)]
    [Header("Input")]
    [Tooltip("Force the OrbitCam to use mouse or touch if i.e. your device is a desktop device but uses mobile controlling (touch)")]
    public InputType InputSelection = InputType.AUTOMATIC;
    public enum InputType { AUTOMATIC, MOUSE, TOUCH}
    private bool useTouch = false;

    //temporary pan position and speed values
    [HideInInspector]
	public Vector3 tempPanPosition;
	[HideInInspector]
	public float velocityPanX;
	[HideInInspector]
	public float velocityPanY;

    private bool doOrbit = false;

    //get the event system
    private EventSystem eventSystem;

	void Start()
	{
        //checks if user forced input to mouse or touch
	    CheckInput();

		//define the angle vector3 and assign the axes
		Vector3 angles = transform.eulerAngles;
		rotationYAxis = angles.y;
		rotationXAxis = angles.x;

        //distance application
	    lerpDistance = distance;
		
		// ensure the rigid body does not change rotation
		if (GetComponent<Rigidbody>())
		{
			GetComponent<Rigidbody>().freezeRotation = true;
		}

        //set stored limit to defined limit first
        storedLimit = yMinLimit;

        //get pan cam
        targetPanCam = GetComponentInChildren<Camera>().gameObject;

        //get eventsystem
        if (EventSystem.current != null)
        {
            eventSystem = EventSystem.current;
        }

	}

    private void CheckInput()
    {
        if (InputSelection == InputType.AUTOMATIC)
        {
#if (UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL)
            useTouch = false;
#endif
#if ((UNITY_ANDROID || UNITY_IOS || UNITY_WP8 || UNITY_WP8_1) && !UNITY_EDITOR)
            useTouch = true;
#endif
        }
        else
        {
            switch (InputSelection)
            {
                    case InputType.MOUSE:
                    useTouch = false;
                    break;
                    case InputType.TOUCH:
                    useTouch = true;
                    break;
            }
        }
    }

    //to get the current rotation before normal orbit cam mode is active again
    public void ResetValues()
    {
        //define the angle vector3 and assign the axes
        Vector3 angles = transform.eulerAngles;
        rotationYAxis = angles.y;
        rotationXAxis = angles.x;
        velocityX = 0f;
        velocityY = 0f;
        velocityPanX = -(tempPanPosition.x / (distance / 10));
        velocityPanY = -(tempPanPosition.y / (distance / 10));
    }

    void Update()
	{
		//check if UI is blocking
        if (eventSystem != null)
        {
            if (EventSystem.current.IsPointerOverGameObject() || EventSystem.current.IsPointerOverGameObject(0) || EventSystem.current.IsPointerOverGameObject(1) || EventSystem.current.IsPointerOverGameObject(2))
            {
                uiBlocking = true;
            }
            else
            {
                uiBlocking = false;
            }
        }
	}

    void CalcPan()
    {
        //function to calculate pan values 
        //on mouse down, calculate the orbital velocity with the given speed values and the axes
        if (UiBlocksInteraction)
        {
            if (!uiBlocking)
            {
                velocityPanX += panSpeed * Input.GetAxis("Mouse X") * 0.2f;
                velocityPanY += panSpeed * Input.GetAxis("Mouse Y") * 0.2f;
            }
        }
        else
        {
            velocityPanX += panSpeed * Input.GetAxis("Mouse X") * 0.2f;
            velocityPanY += panSpeed * Input.GetAxis("Mouse Y") * 0.2f;
        }
    }

    void CalcPanMobile(float veloDeltaX, float veloDeltaY)
    {
        if (UiBlocksInteraction)
        {
            if (!uiBlocking)
            {
                velocityPanX += veloDeltaX;
                velocityPanY += veloDeltaY;
            }
        }
        else
        {
            velocityPanX += veloDeltaX;
            velocityPanY += veloDeltaY;
        }
    }

	void LateUpdate()
	{
		//only if the target exists/is assigned, perform the orbit
		if (target)
		{
		    if (!useTouch)
		    {
		        //ORBIT

		        //define keycodes for orbiting
		        if (Input.GetKey(orbitKey))
		        {
		            doOrbit = true;
		        }
		        else
		        {
		            doOrbit = false;
		        }

		        //for mouse/web/standalone
		        if (doOrbit)
		        {
		            if (UiBlocksInteraction)
		            {
		                if (!uiBlocking)
		                {
		                    //on mouse down, calculate the orbital velocity with the given speed values and the axes
		                    if (!limitToXAxis)
		                        velocityX += xSpeed * Input.GetAxis("Mouse X") * 0.2f;
		                    if (!limitToYAxis)
		                        velocityY += ySpeed * Input.GetAxis("Mouse Y") * 0.2f;
		                }
		            }
		            else
		            {
		                //on mouse down, calculate the orbital velocity with the given speed values and the axes
		                if (!limitToXAxis)
		                    velocityX += xSpeed * Input.GetAxis("Mouse X") * 0.2f;
		                if (!limitToYAxis)
		                    velocityY += ySpeed * Input.GetAxis("Mouse Y") * 0.2f;
		            }

		        }

		        //ZOOM
		        //calculate the distance by checking the mouse scroll an clamp the value to the set distance limits
		        if (enableZooming)
		        {
		            distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * zoomSpeed * 5, distanceMin,
		                distanceMax);
		        }

		    }
		    if (useTouch)
		    {
		        //ORBIT
		        if (Input.touchCount == 1)
		        {
		            if (UiBlocksInteraction)
		            {
		                if (!uiBlocking)
		                {
		                    //on touch down calculate the velocities from the input touch position and the modifyers
		                    if (!limitToXAxis)
		                        velocityX += xSpeed * Input.GetTouch(0).deltaPosition.x *
		                                     (Time.deltaTime / (Input.GetTouch(0).deltaTime + 0.001f)) * 0.01f;
		                    if (!limitToYAxis)
		                        velocityY += ySpeed * Input.GetTouch(0).deltaPosition.y *
		                                     (Time.deltaTime / (Input.GetTouch(0).deltaTime + 0.001f)) * 0.01f;
		                }
		            }
		            else
		            {
		                //on touch down calculate the velocities from the input touch position and the modifyers
		                if (!limitToXAxis)
		                    velocityX += xSpeed * Input.GetTouch(0).deltaPosition.x *
		                                 (Time.deltaTime / (Input.GetTouch(0).deltaTime + 0.001f)) * 0.01f;
		                if (!limitToYAxis)
		                    velocityY += ySpeed * Input.GetTouch(0).deltaPosition.y *
		                                 (Time.deltaTime / (Input.GetTouch(0).deltaTime + 0.001f)) * 0.01f;
		            }
		        }

		        //ZOOM
		        //zooming with tapping / 2 finger gesture
		        if (Input.touchCount == 2)
		        {
		            // Store both touches.
		            Touch touchZero = Input.GetTouch(0);
		            Touch touchOne = Input.GetTouch(1);

		            // Find the position in the previous frame of each touch.
		            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
		            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

		            // Find the magnitude of the vector (the distance) between the touches in each frame.
		            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
		            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

		            // Find the difference in the distances between each frame.
		            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

		            //distance calculation for mobile/touch
		            if (enableZooming)
		            {
		                if (UiBlocksInteraction)
		                {
		                    if (!uiBlocking)
		                    {
		                        distance = Mathf.Lerp(distance,
		                            Mathf.Clamp(distance + deltaMagnitudeDiff * 100000 * zoomSpeed, distanceMin, distanceMax),
		                            Time.deltaTime * smoothTime * 0.1f * zoomSpeed); //FIXME
		                    }
		                }
		                else
		                {
		                    distance = Mathf.Lerp(distance,
		                        Mathf.Clamp(distance + deltaMagnitudeDiff * 100000 * zoomSpeed, distanceMin, distanceMax),
		                        Time.deltaTime * smoothTime * 0.1f * zoomSpeed); //FIXME
		                }
		            }
		        }
		    }

		    //give the calculated values to the rotation axes
            rotationYAxis += velocityX;
			rotationXAxis -= velocityY;

            //clamp the rotation by the set limits and assign the rotation to the x axis
            if (yMinLimit != 0 || yMaxLimit != 0)
                rotationXAxis = Mathf.Clamp(rotationXAxis, yMinLimit, yMaxLimit);
            if (xMinLimit != 0 || xMaxLimit != 0)
                rotationYAxis = Mathf.Clamp(rotationYAxis, xMinLimit, xMaxLimit);

            //define the target rotation (including the calculated rotation axes)
            Quaternion toRotation = Quaternion.Euler(rotationXAxis, rotationYAxis, 0);
			//give over the rotation
            if (useable && EnableOrbiting)
            {
                rotation = toRotation;
            }

            //PAN SETTINGS 
		    if (!useTouch)
		    {
		        //right mouseclick (or selected other keycode) for panning in non-mobile applications

		        //define keycodes for panning
		        if (useable)
		        {
		            if (Input.GetKey(panKey) && enablePanning)
		            {
		                CalcPan();
		            }
		        }

		    }
		    if (useTouch)
		    {
		        //for touch:
		        if (Input.touchCount == 2 && enablePanning && useable && panKey != KeyCode.Mouse0)
		        {

		            //on touch down calculate the velocities from the input touch position and the modifyers
		            float tempVeloXa = panSpeed * Input.GetTouch(0).deltaPosition.x *
		                               (Time.deltaTime / (Input.GetTouch(0).deltaTime + 0.001f)) * 0.05f;
		            float tempVeloXb = panSpeed * Input.GetTouch(1).deltaPosition.x *
		                               (Time.deltaTime / (Input.GetTouch(0).deltaTime + 0.001f)) * 0.05f;
		            float tempVeloYa = panSpeed * Input.GetTouch(0).deltaPosition.y *
		                               (Time.deltaTime / (Input.GetTouch(0).deltaTime + 0.001f)) * 0.05f;
		            float tempVeloYb = panSpeed * Input.GetTouch(1).deltaPosition.y *
		                               (Time.deltaTime / (Input.GetTouch(0).deltaTime + 0.001f)) * 0.05f;

		            float veloDeltaX = (tempVeloXa + tempVeloXb) / 2;
		            float veloDeltaY = (tempVeloYa + tempVeloYb) / 2;

		            CalcPanMobile(veloDeltaX, veloDeltaY);
		        }
		        //if "left-mouse" is selected, disable orbiting and use touch to pan on mobile
		        if (Input.touchCount == 1 && enablePanning && useable && panKey == KeyCode.Mouse0)
		        {
		            float veloDeltaX = panSpeed * Input.GetTouch(0).deltaPosition.x *
		                               (Time.deltaTime / (Input.GetTouch(0).deltaTime + 0.001f)) * 0.05f;
		            float veloDeltaY = panSpeed * Input.GetTouch(0).deltaPosition.y *
		                               (Time.deltaTime / (Input.GetTouch(0).deltaTime + 0.001f)) * 0.05f;

		            CalcPanMobile(veloDeltaX, veloDeltaY);
		        }
		    }



		    //include a raycast for potential other objects (between the target and the cam) obscuring the view

            if (NoObjectsBetween)
            {
                RaycastHit hit;
                if (Physics.Linecast(target.position, transform.position, out hit))
                {
                    float tempDistance = distance;
                    tempDistance -= hit.distance;

                    if (tempDistance < distanceMin)
                    {
                        tempDistance = distanceMin;
                    }
                    distance = Mathf.Lerp(distance, tempDistance, Time.deltaTime * smoothTime * 0.5f);
                }
            }

            //ground hovering: CURRENTLY IN DEVELOPMENT FOR MORE SMOOTHNESS
            if (EnableGroundHovering)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position,Vector3.down, out hit))
                {
                    float storedAngle = rotationXAxis;
                    if (hit.distance > GroundHoverDistance)
                    {

                        yMinLimit = storedLimit;
                    }
                    if (hit.distance < GroundHoverDistance)
                    {
                        yMinLimit = storedAngle;
                        rotationXAxis = storedAngle;
                    }

                }
            }


            //set the temporary position
            if (target != null)
            {
                //lerp the distance
                lerpDistance = Mathf.Lerp(lerpDistance, distance, smoothTime * Time.deltaTime);

                //create the inverted distance to move the cam away from the object
                Vector3 negDistance = new Vector3(0.0f, 0.0f, -lerpDistance);

                Vector3 position = rotation * negDistance + target.position;
                //create yet another vec3 to include the defined offset
                Vector3 offsetPosition = new Vector3(position.x + xOffset, position.y + yOffset, position.z);

                //finaly set the transform by giving over the temporary position/rotation to the object transform
                transform.rotation = rotation;
                transform.position = offsetPosition;
            }

			//orbiting mode
			if (enableAutomaticOrbiting == true)
			{
				velocityX = Mathf.Lerp(velocityX,orbitingSpeed, Time.deltaTime * smoothTime);
			}

            //assign the smoothing effect to the velocity with lerp
            velocityX = Mathf.Lerp(velocityX, 0, Time.deltaTime * smoothTime);
            velocityY = Mathf.Lerp(velocityY, 0, Time.deltaTime * smoothTime);

            //panning
            tempPanPosition = new Vector3(-velocityPanX * distance / 10, -velocityPanY * distance / 10, 0);

            //apply panning
            if (targetPanCam != null && useable && enablePanning)
            {
                targetPanCam.transform.localPosition = Vector3.Lerp(targetPanCam.transform.localPosition, tempPanPosition, Time.deltaTime * smoothTime * 1.5f);
                //pan limitations
                if (LimitPan) 
                {
                    float clampX = Mathf.Clamp(targetPanCam.transform.localPosition.x, PanLimitsLeftRight.x, PanLimitsLeftRight.y);
                    float clampY = Mathf.Clamp(targetPanCam.transform.localPosition.y, PanLimitsUpDown.x, PanLimitsUpDown.y);
                    targetPanCam.transform.localPosition = new Vector3(clampX, clampY, targetPanCam.transform.localPosition.z);
                }
            }
		}

	}
}
