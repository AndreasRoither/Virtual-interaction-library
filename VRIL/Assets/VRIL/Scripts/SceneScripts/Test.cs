//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using VRIL.Callable;
//using VRIL.ControllerActionEventArgs;
//using VRIL.Interactable;
//using VRIL.InteractionTechniques;
//using VRIL.Manager;

//public class Test : VRIL_Callable
//{
//    bool active;

//    public VRIL_Manager Manager;

//    public override void OnCall()
//    {
//        active = true;
//    }

//    private void OnCollisionEnter(Collision collision)
//    {
//        if(active)
//        {
//            VRIL_Interactable inter = gameObject.GetComponent<VRIL_Interactable>();
            
//            foreach (VRIL_RegisteredController t in Manager.RegisteredControllers)
//            {
//                foreach(VRIL_InteractionTechniqueBase i in t.InteractionTechniques)
//                {
//                    Debug.Log(i.GetSelectedObjects());
//                    foreach (VRIL_Interactable selected in i.GetSelectedObjects())
//                    {
//                        if(selected.gameObject.GetInstanceID() == gameObject.GetInstanceID())
//                        {
//                            i.OnRelease(null);
//                        }
//                    }
//                }
//            }
//        }   
//    }

//    public override void OnStop()
//    {
//        throw new System.NotImplementedException();
//    }

//    // Start is called before the first frame update
//    void Start()
//    {
        
//    }

//    // Update is called once per frame
//    void Update()
//    {   
//    }
//}
