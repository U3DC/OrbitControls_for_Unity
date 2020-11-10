using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using THOK.Tools.Common;
/* ========================================================================================================
 * 70:30 SmoothOrbitViewchanger Script - created by D.Michalke / 70:30 / http://70-30.de / info@70-30.de
 * used to trigger a camera perspective change for Smooth!
 * possible triggers: by click/touch on object, UI element with this script on it - OR - by calling public function TriggerViewchange()
 * ========================================================================================================
 */
public class SmoothOrbitViewchanger : MonoBehaviour {

    //the data for the viewchange: enter in inspector
    public Vector3 CamRotation;

    private Quaternion RotaQuat;
    public float CamDistance;

    public Vector2 CamPanValues;

    //speed
    public float CamMovingSpeed = 1;

    //to get the camera control script
    private SmoothOrbitCam smoothOrbitCam;

    //movement bool
    private bool moving = false;

    private Transform tempTarget;

	void Start ()
    {
        //get camera system
        smoothOrbitCam = FindObjectOfType<SmoothOrbitCam>().gameObject.GetComponent<SmoothOrbitCam>();
        RotaQuat.eulerAngles = CamRotation;

        //apply speed
        CamMovingSpeed = CamMovingSpeed / 10;

        // EventCenter.AddListener("DoubleClik", onTest);
        _prevouseClick = Time.realtimeSinceStartup;
    }



    private float _prevouseClick;

    void Update ()
    {

        if (Input.GetMouseButtonUp(0))
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit) && hit.transform == transform)
            {
                if ((Time.realtimeSinceStartup - _prevouseClick) < 0.2f)
                {
                    //注意：要双击的物体上一定要有碰撞器，并且碰撞器和本脚本挂在同一个物体上
                    Debug.Log("双击");
                    onTest();

                }
                else
                {
                    _prevouseClick = Time.realtimeSinceStartup;
                }
            }
        }

        if (moving)
        {
            tempTarget.position = Vector3.Lerp(tempTarget.position, transform.position, CamMovingSpeed);


            //            get origin values//lerp to target values
            Quaternion rot = Quaternion.Lerp(smoothOrbitCam.transform.rotation,RotaQuat, CamMovingSpeed);
            float dis = Mathf.Lerp(smoothOrbitCam.distance, CamDistance, CamMovingSpeed);
            Vector3 pan = Vector3.Lerp(smoothOrbitCam.targetPanCam.transform.localPosition,new Vector3(CamPanValues.x, CamPanValues.y,0), CamMovingSpeed);
            rot.eulerAngles = new Vector3(rot.eulerAngles.x, rot.eulerAngles.y, 0);

            smoothOrbitCam.rotation = rot;
            smoothOrbitCam.distance = dis;
            smoothOrbitCam.targetPanCam.transform.localPosition = pan;
        }
	}

    //public void OnPointerUp(PointerEventData e)
    //{
    //   // StartCoroutine(ViewChange());
    //}

    void onTest()
    {
        if (smoothOrbitCam.UiBlocksInteraction && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
           return;
        }
        StartCoroutine(ViewChange());
    }

    public void TriggerViewChange() //if the viewchange should be called from code or from a UI button somewhere
    {
        StartCoroutine(ViewChange());
    }

    private IEnumerator ViewChange()
    {
        //clean existing cam system values
        //smoothOrbitCam.ResetValues();

        if (GameObject.Find("TemporarySmoothOrbitCamTarget") == true) yield break;

        //perform
        moving = true;
        smoothOrbitCam.useable = false;

        GameObject go = new GameObject();
        go.name = "TemporarySmoothOrbitCamTarget";

        tempTarget = go.transform;
        tempTarget.position = smoothOrbitCam.target.transform.position;
        //switch target
        smoothOrbitCam.target = tempTarget;

        //wait for the movement to finish
        yield return new WaitForSeconds(1.2f);

        //switch target
        smoothOrbitCam.target = transform;

        Destroy(go);

        //stop performing
        moving = false;
        smoothOrbitCam.useable = true;

        //overwrite existing values
        //avoid reset of the pan value after viewchange
        smoothOrbitCam.tempPanPosition = CamPanValues;
        //clean values again to give them free for the normal controls again
        smoothOrbitCam.ResetValues();
    }
}
