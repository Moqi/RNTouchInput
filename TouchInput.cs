// Copyright (c) 2012 Xilin Chen (RN)
// Please direct any bugs/comments/suggestions to http://blog.sina.com.cn/u/2840185437

using UnityEngine;
using System.Reflection;


/// <summary>
/// TouchInput
/// Please add this script to Main Camera in editor.
/// 
/// 
/// --------------------------------------------------
/// Set TouchInput property in Inspector
/// 
/// touchLayerMask : Used to touch only a part of the scene. Default is Everything.  
/// 
/// stationaryTouchEnable : Enable the stationary touch.
///                         Send the move touch message(onTouchMove) if the touch is not move.
///                         
/// touchEnterExitEnable : Enable the enter and exit touch.
///                        Send the onTouchEnter and onTouchExit message if the touch enter or exit the GameObject.
///                        
/// cameras : From the camera to send a touch message.
///           set the Main Camera if this member variables is None.
/// 
/// 
/// --------------------------------------------------
/// //Overridable Functions
/// void onTouchDown(Vector3 position)
/// {
/// }
/// 
/// void onTouchMove(Vector3 position)
/// {
/// }
/// 
/// void onTouchUpAsButton(Vector3 position)
/// {
/// }
/// 
/// void onTouchUp(Vector3 position)
/// {
/// }
/// 
/// void onTouchEnter(Vector3 position)
/// {
/// }
/// 
/// void onTouchExit(Vector3 position)
/// {
/// }
/// 
/// </summary>
public class TouchInput : MonoBehaviour
{
    protected GameObject[] _touchGOs = new GameObject[] 
    {
        null, null, null, null, null,
        null, null, null, null, null,
        null, null, null, null, null,
        null, null, null, null, null
    };
    protected Camera[] _touchCameras = new Camera[] 
    {
        null, null, null, null, null,
        null, null, null, null, null,
        null, null, null, null, null,
        null, null, null, null, null
    };
    protected GameObject[] _touchEEGOs;
    protected Camera[] _touchEECameras;

    Vector3[] _lastMousePos = new Vector3[3];


    //
    /// <summary>
    /// Touch layer mask.
    /// Default is -1.
    /// </summary>
    public LayerMask touchLayerMask = -1;
    /// <summary>
    /// Enable the stationary touch.
    /// Send the move touch message(onTouchMove) if the touch is not move.
    /// </summary>
    public bool stationaryTouchEnable = true;
    /// <summary>
    /// Enable the enter and exit touch.
    /// Send the onTouchEnter and onTouchExit message if the touch enter or exit the GameObject.
    /// </summary>
    public bool touchEnterExitEnable = false;
    /// <summary>
    /// From the camera to send a touch message.
    /// Set the Main Camera if this member variables is None.
    /// </summary>
    public Camera[] cameras;
    


    /// <summary>
    /// Touch points from the camera.
    /// </summary>
    public static Camera currentCamera;
    /// <summary>
    /// Current touch ray by the current camera
    /// </summary>
    public static Ray ray;
    /// <summary>
    /// Current raycast hit info.
    /// </summary>
    public static RaycastHit raycastHit;

    /// <summary>
    /// Current touch position
    /// </summary>
    public static Vector3 position;
    /// <summary>
    /// Current touch delta position by the last touch.
    /// </summary>
    public static Vector3 deltaPosition;
    /// <summary>
    /// Current touch delta time by the last touch.
    /// </summary>
    public static float deltaTime;
    /// <summary>
    /// Current finger id
    /// </summary>
    public static int fingerId;
    /// <summary>
    /// Current touch phase
    /// </summary>
    public static TouchPhase phase;
    /// <summary>
    /// Current tap count
    /// </summary>
    public static int tapCount;




    /// <summary>
    /// Copies the touch data.
    /// </summary>
    void copyTouch(Touch touch)
    {
        position = touch.position;
        deltaPosition = touch.deltaPosition;
        deltaTime = touch.deltaTime;
        fingerId = touch.fingerId;
        phase = touch.phase;
        tapCount = touch.tapCount;
    }



    //
    void Awake()
    {
        //
        if (cameras.Length == 0)
        {
            var cs = GameObject.FindObjectsOfType(typeof(Camera));
            cameras = new Camera[cs.Length];

            var i = 0;
            foreach (var c in cs)
            {
                var cc = c as Camera;
                cameras[i++] = cc;
                //print("currentCamera=" + cc);
            }
        }


        //
        if (touchEnterExitEnable)
        {
            _touchEEGOs = new GameObject[] 
            {
                null, null, null, null, null,
                null, null, null, null, null,
                null, null, null, null, null,
                null, null, null, null, null
            };
            _touchEECameras = new Camera[] 
            {
                null, null, null, null, null,
                null, null, null, null, null,
                null, null, null, null, null,
                null, null, null, null, null
            };
        }
    }


    //
    void Update()
    {

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            
        mouseInput();//法克鱿 原来Input.GetMouse*** 能得到touch的数据

#elif UNITY_IPHONE || UNITY_ANDROID

        foreach (UnityEngine.Touch touch in Input.touches)
        {
            copyTouch(touch);
            touchHandle();
        }

#endif

    }


    /// <summary>
    /// handle the touch message.
    /// </summary>
    public void touchHandle()
    {
        /*print("tapCount=" + tapCount
            + "  fingerId=" + fingerId
            + "  phase=" + phase
            + "  deltaTime=" + deltaTime
            + "  position=" + position
            + "  deltaPosition=" + deltaPosition);
        */

        //
        if (fingerId >= 20)
            return;

        //
        if (phase == TouchPhase.Canceled)
        {
            _touchGOs[fingerId] = null;
        }
        else if (phase == TouchPhase.Began)
        {
            onBegan();
        }
        else if (phase == TouchPhase.Stationary)
        {
            if(stationaryTouchEnable)
                onMoved();
        }
        else if (phase == TouchPhase.Moved)
        {
            onMoved();
        }
        else if (phase == TouchPhase.Ended)
        {
            onEnded();
        }
    }


    /// <summary>
    /// handle the Began touch message.
    /// </summary>
    void onBegan()
    {
        foreach (var c in cameras)
        {
            //
            currentCamera = c;
            ray = currentCamera.ScreenPointToRay(position);

            //
            if (Physics.Raycast(ray, out raycastHit, currentCamera.far, touchLayerMask))
            {
                var go = raycastHit.collider.gameObject;
                _touchGOs[fingerId] = go;
                _touchCameras[fingerId] = currentCamera;
                dispatchMessage(go, "onTouchDown", position);

                break;
            }
        }
    }


    /// <summary>
    /// handle the Move touch message.
    /// </summary>
    void onMoved()
    {
        var tgo = _touchGOs[fingerId];
        if (tgo != null)
        {
            currentCamera = _touchCameras[fingerId];
            ray = currentCamera.ScreenPointToRay(position);
            dispatchMessage(tgo, "onTouchMove", position);
        }

        //
        touchEnterExit();
    }


    /// <summary>
    /// handle the end touch message.
    /// </summary>
    void onEnded()
    {
        //
        if (touchEnterExitEnable)
        {
            var go = _touchEEGOs[fingerId];
            if (go != null)
            {
                dispatchMessage(go, "onTouchExit", position);
                _touchEEGOs[fingerId] = null;
                _touchEECameras[fingerId] = null;
            }
        }



        //
        var tgo = _touchGOs[fingerId];
        if (tgo == null)
            return;

        _touchGOs[fingerId] = null;
        currentCamera = _touchCameras[fingerId];

        _touchCameras[fingerId] = null;
        ray = currentCamera.ScreenPointToRay(position);

        //
        if (Physics.Raycast(ray, out raycastHit, currentCamera.far, touchLayerMask))
        {
            var go = raycastHit.collider.gameObject;
            if (go == tgo)
            {
                dispatchMessage(go, "onTouchUpAsButton", position);
                dispatchMessage(go, "onTouchUp", position);
                return;
            }
        }


        //
        dispatchMessage(tgo, "onTouchUp", position);
    }



    /// <summary>
    /// handle the enter and exit touch message.
    /// </summary>
    void touchEnterExit()
    {
        if (touchEnterExitEnable)
        {
            var last_go = _touchEEGOs[fingerId];
            if (last_go != null)
            {
                currentCamera = _touchEECameras[fingerId];

                //
                ray = currentCamera.ScreenPointToRay(position);


                //
                if (Physics.Raycast(ray, out raycastHit, currentCamera.far, touchLayerMask))
                {
                    var go = raycastHit.collider.gameObject;
                    if (last_go != go)
                    {
                        dispatchMessage(last_go, "onTouchExit", position);

                        dispatchMessage(go, "onTouchEnter", position);
                        _touchEEGOs[fingerId] = go;
                        //_touchEECameras[fingerId] = currentCamera;
                    }
                }
                else //if (last_go != null)
                {
                    dispatchMessage(last_go, "onTouchExit", position);
                    _touchEEGOs[fingerId] = null;
                    _touchEECameras[fingerId] = null;
                }
            }
            else
            {
                foreach (var c in cameras)
                {
                    //
                    currentCamera = c;
                    ray = currentCamera.ScreenPointToRay(position);

                    //
                    if (Physics.Raycast(ray, out raycastHit, currentCamera.far, touchLayerMask))
                    {
                        var go = raycastHit.collider.gameObject;

                        dispatchMessage(go, "onTouchEnter", position);

                        _touchEEGOs[fingerId] = go;
                        _touchEECameras[fingerId] = currentCamera;
                        break;
                    }
                }
            }
        }
    }


    /// <summary>
    /// handle the mouse input message.
    /// </summary>
    void mouseInput()
    {
        for (var i = 0; i < 3; ++i)
        {
            if (Input.GetMouseButtonDown(i))
            {
                fingerId = i;
                position = Input.mousePosition;
                phase = TouchPhase.Began;
                deltaPosition = Vector3.zero;
                deltaTime = Time.deltaTime;
                tapCount = 0;

                _lastMousePos[i] = position;
                touchHandle();
            }
            else if (Input.GetMouseButton(i))
            {
                if (_lastMousePos[i] == Input.mousePosition)
                {
                    fingerId = i;
                    position = Input.mousePosition;
                    phase = TouchPhase.Stationary;
                    deltaPosition = Vector3.zero;
                    deltaTime = Time.deltaTime;
                    tapCount = 1;

                    _lastMousePos[i] = position;
                    touchHandle();
                }
                else
                {
                    fingerId = i;
                    position = Input.mousePosition;
                    phase = TouchPhase.Moved;
                    deltaPosition = position - _lastMousePos[i];
                    deltaTime = Time.deltaTime;
                    tapCount = 1;

                    _lastMousePos[i] = position;
                    touchHandle();
                }
            }
            else if (Input.GetMouseButtonUp(i))
            {
                fingerId = i;
                position = Input.mousePosition;
                phase = TouchPhase.Ended;
                deltaPosition = position - _lastMousePos[i];
                deltaTime = Time.deltaTime;
                tapCount = 0;

                _lastMousePos[i] = position;
                touchHandle();
            }
            else
            {
            }
        }
    }



    /// <summary>
    /// dispatch message to the scripts.
    /// </summary>
    static void dispatchMessage(GameObject self, string fun, params object[] values)
    {
        var mbs = self.GetComponents<MonoBehaviour>();

        foreach (var mb in mbs)
        {
            var f = mb.GetType().GetMethod(fun, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            if (f != null)
            {
                f.Invoke(mb, values);
            }
        }
    }

}
