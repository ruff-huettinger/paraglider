using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParagliderControler : MonoBehaviour {

    public AeroplaneController aeroplaneController;
    public AeroplaneUserControl userControl;
    public paragliderFogLayer fogLayer;
    public Rigidbody rigi;
    public Transform dockingTarget;	//center of finish collider
	public float dockingDistance;	//cam distance to finish collider, where to jump to nect level
    public bool isDocking = false;
	Transform dockingOrigin;
    Vector3 dockingStartVelocity;
    public float maxDockingTime = 2f;
    public float dockingTime = 0f;

	public Vector3	position ;	//geschwindigkeit des gliders in m/s
	public float   speed  ;		
	public float   altitude ;	//aktuellle höhe
	public float	altChange ; //änderung der höhe

    public string finishColliderName = "finishCollider";
    public string crashColliderName = "crashCollider";
    public string fogColliderName = "fogCollider";
    public string spawnCollider = "spawnCollider";
    public string thermicUpName = "thermicUpCollider";
    public string thermicDownName = "thermicDownCollider";

    public delegate void Delegate();
    public Delegate onCrash;
    //public Delegate onFog;
	public Delegate onDocking;
    public Delegate onFinishReached;
    public Delegate onSpawn;
    public Delegate onThermicUp;
    public Delegate onThermicDown;
    public Delegate onThermicLeave;
    public Delegate onThermicDisappear;
	public Delegate onThermicAppear;

    public float respawnTimePanalalty = 5f;
    public float respawnTimer = 0;
    public float respawnHeight = 150f;
    public float maxHeight = 175f;
    public List<Vector3> respawnCollector = new List<Vector3>();
    public float forceOnThermicUp = 1000;
    public float forceOnThermicDown = -500f;
    public Vector3 forceByThermic = new Vector3();
    public float forceOnHeight = -500f;
    public bool disableCrashedObjectOnCrash = false;
    public bool disableSpawnColliderOnTrigger = true;
    public bool disableWindColliderOnTrigger = false;
    private bool thermicsEnabled = true;



    // Use this for initialization
    void Start() {
		if(  aeroplaneController ==null)
        	aeroplaneController = gameObject.GetComponent<AeroplaneController>();
		if(  userControl ==null)
        	userControl = gameObject.GetComponent<AeroplaneUserControl>();
		if(  fogLayer ==null)
			fogLayer = gameObject.GetComponentInChildren<paragliderFogLayer>();

		GameObject ndp = new GameObject();
		ndp.name= "next finish docking point";
		nextDockingPoint = ndp.transform;
		nextDockingPoint.parent=transform.parent;
		if(  rigi ==null)
        	rigi = GetComponent<Rigidbody>();
    }

	// Update is called once per frame
	void Update () {

		if(isDocking)
		{
        	doDocking();
        }
		updateRespawn();

	position =transform.position;
	speed = rigi.velocity.magnitude;
	altitude = transform.position.y;
	altChange = rigi.velocity.y;

	}



    private void FixedUpdate()
    {
    	if(!isDocking)
   		{
	        if (transform.position.y > maxHeight)
	        {
	            rigi.AddForce(new Vector3(0, forceOnHeight, 0), ForceMode.Acceleration);
	        }
	        else
	        {
	            rigi.AddForce(forceByThermic, ForceMode.Acceleration);
	        }
	     }
    }



/*
       █▀▀▄  ▄▀▀▄  ▄▀▀▄  █ ▄▀  ▀█▀  █▄ █  ▄▀▀▄ 
       █  █  █  █  █     ██     █   █▀▄█  █ ▄▄ 
       █▄▄▀  ▀▄▄▀  ▀▄▄▀  █ ▀▄  ▄█▄  █ ▀█  ▀▄▄▀ 
*/
	public Transform nextDockingPoint;

	void startDocking(ParagliderFinish finish)
    {
		if(finish==null)
    	{
			Debug.LogError("WARNING: no docket target found, there must be a problem with the ParagliderFinish");
    	}
    	togglePhysics(false);
		dockingTarget = finish.transform;
		dockingDistance = Vector3.Distance(finish.transform.position, finish.dockingPoint.position); //doesnt need to be that point because target is billboard 
    	dockingTime = maxDockingTime;
		

		// find the next point between position and dockingTarget in dockingDistance from dockingTarget
		nextDockingPoint.position = Vector3.MoveTowards(dockingTarget.position, transform.position, dockingDistance);
		// find rotation towards docking target
		nextDockingPoint.LookAt (dockingTarget.position);

		onDocking();
    	isDocking = true;
    }

    void doDocking()
    {

		float t = Mathf.Pow(Mathf.InverseLerp(maxDockingTime,0,dockingTime),1);
//		float t2 = Mathf.Clamp01(Mathf.InverseLerp(maxDockingTime,maxDockingTime-1,dockingTime));



		//lerp the position
		transform.position = Vector3.Lerp(transform.position, 	nextDockingPoint.position, 	t);
		//lerp the rotation
		transform.eulerAngles = BenjasMath.angularLerp(transform.eulerAngles,nextDockingPoint.eulerAngles,t);
		// lerp the velocity
		//rigi.velocity = Vector3.Lerp(rigi.velocity , 	Vector3.zero, 	t2);

		float dist = Vector3.Distance(transform.position,nextDockingPoint.position);
		float angularDist = Vector3.Distance(transform.eulerAngles,nextDockingPoint.eulerAngles);
		//test if docking time is over or position is close enough (10mm and 1/10°)
		if(BenjasMath.countdownToZero(ref dockingTime) || (dist<0.01 && angularDist<0.1))
		{
			finishDocking();
		}
    }

	void finishDocking()
    {
		transform.position = nextDockingPoint.position;
		transform.eulerAngles = nextDockingPoint.eulerAngles;
		rigi.velocity = Vector3.zero;
		//togglePhysics(true);
		isDocking = false;
		onFinishReached();
    }

/*
       █▀▀▄  █  █  █  █  ▄▀▀▀  ▀█▀  ▄▀▀▄  ▄▀▀▀ 
       █▀▀   █▀▀█  █▄▀   ▀▀▀▄   █   █     ▀▀▀▄ 
       █     █  █  █     ▀▄▄▀  ▄█▄  ▀▄▄▀  ▀▄▄▀ 
*/

    private  float physicsSpeedBackup = 0;
    private bool physicsOn = true;

    public bool physicsEnabled()
    {
    	return physicsOn;
    }


	public void togglePhysics()
	{
		togglePhysics(!physicsOn);
	}

    public void togglePhysics(bool on)
    {
    	if(physicsOn != on)
    	{
			aeroplaneController.enabled=on;
			userControl.enabled=on;
			rigi.useGravity = on;
			if(on )
			{
				//restore the speed in forward direction
				rigi.isKinematic = false;
				rigi.velocity = transform.forward*physicsSpeedBackup;
				rigi.angularVelocity *=0;
			}
			else 
			{
				//store the current speed before setting it 0;
				rigi.isKinematic = true;
				physicsSpeedBackup=rigi.velocity.magnitude;
				rigi.velocity *= 0;
				rigi.angularVelocity *=0;
			}
			physicsOn=on;
		}
    }



/*
       ▄▀▀▄  ▄▀▀▄  █     ▀█▀  ▄▀▀▀  ▀█▀  ▄▀▀▄  █▄ █ 
       █     █  █  █      █   ▀▀▀▄   █   █  █  █▀▄█ 
       ▀▄▄▀  ▀▄▄▀  █▄▄▄  ▄█▄  ▀▄▄▀  ▄█▄  ▀▄▄▀  █ ▀█ 
*/

	void OnCollisionEnter(Collision collision)
    {
        onCrash();
        if(disableCrashedObjectOnCrash)
        collision.collider.enabled = false;
    }


    void OnTriggerEnter(Collider col)
    {
        Debug.Log("-----Triggered " + col.gameObject.name   );
        if (col.gameObject.name == finishColliderName)
        {
        	//this is a bit quick n dirty
			startDocking(col.transform.parent.parent.GetComponent<ParagliderFinish>());
        }
        else if (col.gameObject.name == fogColliderName)
        {
            //onFog();
            fogLayer.doFog();
        }
        else if (col.gameObject.name == spawnCollider)
        {
            onSpawn();
            col.enabled = !disableSpawnColliderOnTrigger;
        }
        else if (col.gameObject.name == thermicUpName)
        {
            onThermicUp();
            setThermic(1);
            if(disableWindColliderOnTrigger)
                col.transform.parent.gameObject.SetActive(false);
        }
        else if (col.gameObject.name == thermicDownName)
        {
            onThermicDown();
            setThermic(-1);
            if(disableWindColliderOnTrigger)
                col.transform.parent.gameObject.SetActive(false);
        }
    }

    void OnTriggerExit(Collider col)
    {
        if (col.gameObject.name == thermicUpName)
        {
            onThermicLeave();
            setThermic(0);
        }
        else if (col.gameObject.name == thermicDownName) onThermicLeave();
    }

    public void toggleThermics(bool enabled)
    {
        if (enabled == false)
            setThermic(0);
        thermicsEnabled = enabled;
    }


    public void setThermic(int direction) //-1 down, 0 normal, +1 up
    {
        forceByThermic = Vector3.zero;
        if (thermicsEnabled)
        {
            if (direction > 0 )
                forceByThermic.y = forceOnThermicUp;
            else if (direction < 0)
                forceByThermic.y = forceOnThermicDown;
        }
    }


/*
       ▄▀▀▀  █▀▀▄  ▄▀▀▄  █   █  █▄ █  ▀█▀  █▄ █  ▄▀▀▄ 
       ▀▀▀▄  █▀▀   █▀▀█  █ █ █  █▀▄█   █   █▀▄█  █ ▄▄ 
       ▀▄▄▀  █     █  █   █ █   █ ▀█  ▄█▄  █ ▀█  ▀▄▄▀ 
*/

	public void resetRespawn()
    {
		respawnCollector.Clear();
		respawnCollector.Add (respawnInfo(transform));
    }

    public void updateRespawn()
    {
    	//save glider position every second
    	respawnTimer-=Time.deltaTime;
    	if (respawnTimer<0)
    	{ 
    		respawnTimer = 1f;
    		respawnCollector.Add (respawnInfo(transform));
    	}

		//keep glider positions for a time of maxTimePanalalty only
		if(respawnCollector.Count>respawnTimePanalalty)
		{
			respawnCollector.RemoveAt(0);
		}
    }

    public void respawn()
    {
	
    	//set glider To last respawn position in horizontal direction and flight height
		Vector3 position = respawnCollector[0];
		Vector3 rotation = respawnCollector[0];
    	rotation.x=0;
		rotation.z=0;
		position.y = respawnHeight;
		spawnGlider(position,rotation);
		resetRespawn(); //prevents from  being set into a crash situation

    }

	public Vector3 respawnInfo(Transform trafo)
    {
		Vector3 respawnInfo = transform.position;
    	respawnInfo.y = transform.eulerAngles.y; //save horizontal rotation instad of height;
    	return respawnInfo;
    }

	public void spawn(Transform trafo)
	{
        if (trafo == null)
        {
            Debug.LogError("no transform delivered, spawning at zero");
            spawnGlider(Vector3.zero, Vector3.zero);
        }
        else
        {
            spawnGlider(trafo.position, trafo.eulerAngles);
        }

		//aeroplaneController.Reset();
		//userControl.Reset();
	}


    public void spawnGlider(Vector3 position, Vector3 rotation)
    {
		transform.position=position;
		transform.eulerAngles=rotation;
        rigi.angularVelocity = Vector3.zero;
        Debug.Log("Spawning Glider at "+transform.position);
    }
}
