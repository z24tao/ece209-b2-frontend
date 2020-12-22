using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Car : MonoBehaviour {
	[System.Serializable]
	public class Data {
		public Space.SpaceData goal;
		public List<Space.SpaceData> obstacles;
		public int ctrl1, ctrl2;

		public Data(Space _goal, List<Space> _obstacles, int _ctrl1, int _ctrl2) {
			goal = _goal.data;
			obstacles = new List<Space.SpaceData>();
			for (int i = 0; i < _obstacles.Count; i ++) {
				obstacles.Add(_obstacles[i].data);
			}
			ctrl1 = _ctrl1;
			ctrl2 = _ctrl2;
		}
	}

	public GameObject goal;
	public GameObject obstacle;

	private Space goalInst;
	private List<Space> obstacles = new List<Space> ();

	private int frameNum = 0;
	private bool started = false;

	private int speed = 0;
	private int maxSpeed = 10;
	private float dir = 90;
	private float maxDir = 45;
	private bool locked = false;

	private bool auto = false;
	private int auto_collect_count = 0;
	private string baseURL = "http://localhost:8080/";
	private bool autoUp = false;
	private bool autoDown = false;
	private bool autoLeft = false;
	private bool autoRight = false;

	// Use this for initialization
	void Start () {
		SetGoal (50f, 0f, 15f, 10f);

		float[][] obstacleConfig = new float[][] {
			new float[] { 20f, 0f, 5f, 30f },
			new float[] { -40f, 0f, 5f, 100f },
			new float[] { 10f, 50f, 100f, 5f },
			new float[] { 10f, -50f, 100f, 5f },
			new float[] { 60f, 0f, 5f, 100f },
			new float[] { 50f, 20f, 15f, 10f },
			new float[] { 50f, -20f, 15f, 10f },
//			new float[] { 50f, 30f, 15f, 10f },
//			new float[] { 50f, -30f, 15f, 10f },
//			new float[] { 50f, -20f, 12f, 7f },
//			new float[] { 10f, -20f, 12f, 7f },



		};

		for (int i = 0; i < obstacleConfig.Length; i++) {
			AddObstacle (obstacleConfig[i][0], obstacleConfig[i][1], obstacleConfig[i][2], obstacleConfig[i][3]);
		}

		obstacles [5].GetComponent<Renderer> ().material.SetColor ("_Color", Color.blue);
		obstacles [6].GetComponent<Renderer> ().material.SetColor ("_Color", Color.blue);
	}

	void SetGoal(float xpos, float zpos, float xdim, float zdim) {
		goalInst = GameObject.Instantiate (goal).GetComponent<Space>();
		goalInst.transform.position = new Vector3 (xpos, 0f, zpos);
		goalInst.transform.localScale = new Vector3 (xdim, 1f, zdim);
		goalInst.SetAttrs ();
	}

	void AddObstacle(float xpos, float zpos, float xdim, float zdim) {
		Space obstacleInst = GameObject.Instantiate (obstacle).GetComponent<Space>();
		obstacleInst.transform.position = new Vector3 (xpos, 0f, zpos);
		obstacleInst.transform.localScale = new Vector3 (xdim, 1f, zdim);
		obstacleInst.SetAttrs ();
		obstacles.Add (obstacleInst);
	}
	
	// Update is called once per frame
	void Update () {
		if (auto) {
			GameObject.Find ("Text").GetComponent<Text> ().text = "AUTO MODE";
			Auto ();
		} else {
			GameObject.Find ("Text").GetComponent<Text> ().text = "TRAINING MODE";
			Control ();
		}
		Drive ();
	}

	void Auto() {
		if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) ||
			Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) ||
			Input.GetKey (KeyCode.S)) {
			auto = false;
			Control ();
			return;
		}

		StartCoroutine (autoStep());
		autoControl ();
	}

	void autoReset() {
		autoUp = false;
	    autoDown = false;
	    autoLeft = false;
	    autoRight = false;
	}

	IEnumerator autoStep() {
		frameNum = (frameNum + 1) % 10;
		if (frameNum == 0) {
			var stepURL = baseURL + "distance";
			stepURL += "?srcx=" + transform.position.x;
			stepURL += "&srcy=" + transform.position.z;
			stepURL += "&dir=" + (transform.eulerAngles.y + 270) % 360;
			stepURL += "&ctrl=" + (dir + 270) % 360;

			var stepData = JsonUtility.ToJson (new Data (goalInst, obstacles, 0, 0));
			var req = new UnityWebRequest (stepURL, "POST");
			byte[] bodyRaw = Encoding.UTF8.GetBytes (stepData);
			req.uploadHandler = (UploadHandler)new UploadHandlerRaw (bodyRaw);
			req.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer ();
			yield return req.SendWebRequest ();

			if (req.isNetworkError || req.isHttpError) {
				Debug.Log (req);
			} else {
				print (req.downloadHandler.text);
				if (req.downloadHandler.text [0] == '+') {
					autoUp = true;
					autoDown = false;
				} else if (req.downloadHandler.text [0] == '-') {
					autoUp = false;
					autoDown = true;
				} else {
					autoUp = false;
					autoDown = false;
				}
				if (req.downloadHandler.text [1] == '+') {
					autoRight = true;
					autoLeft = false;
				} else if (req.downloadHandler.text [1] == '-') {
					autoRight = false;
					autoLeft = true;
				} else {
					autoRight = false;
					autoLeft = false;
				}
			}
		} else {
			yield return new WaitForSeconds (0);
		}
	}

	void autoControl() {
		if (autoUp) {
			speed = maxSpeed;
		} else if (autoDown) {
			speed = -maxSpeed;
		}

		if (speed > maxSpeed) {
			speed = maxSpeed;
		} else if (speed < 0 - maxSpeed) {
			speed = 0 - maxSpeed;
		}

		if (autoLeft) {
			locked = false;
			dir -= 5f;
			if (dir < 0) {
				dir += 360;
			}
		} else if (autoRight) {
			locked = false;
			dir += 5f;
			if (dir > 360) {
				dir -= 360;
			}
		}

		int carDir = (int)(transform.eulerAngles.y);
		if (carDir - dir > 180) {
			dir += 360;
		} else if (dir - carDir > 180) {
			carDir += 360;
		}
		if (dir > carDir + maxDir) {
			dir = carDir + maxDir;
		} else if (dir < carDir - maxDir) {
			dir = carDir - maxDir;
		}
	}

	void Control () {
		int ctrl1 = 0;
		int ctrl2 = 0;

		if (started) {
			GameObject.Find ("Intro").GetComponent<Text> ().text = "";
			GameObject.Find ("Image").GetComponent<Image> ().color = new Color32(255,255,225,0);
			auto_collect_count ++;
		}

		if (Input.GetKey (KeyCode.A)) {
			started = true;
			auto = true;
			auto_collect_count = 0;
			return;
		}

		if (Input.GetKey(KeyCode.UpArrow)) {
			started = true;
			speed = maxSpeed;
			ctrl1 = 1;
		} else if (Input.GetKey(KeyCode.DownArrow)) {
			started = true;
			speed = -maxSpeed;
			ctrl1 = -1;
		}

		if (speed > maxSpeed) {
			speed = maxSpeed;
		} else if (speed < 0 - maxSpeed) {
			speed = 0 - maxSpeed;
		}

		if (Input.GetKey (KeyCode.S)) {
			locked = true;
			speed = 0;
			ctrl1 = 0;
		}

		if (Input.GetKey (KeyCode.LeftArrow)) {
			started = true;
			ctrl2 = -1;
			locked = false;
			dir -= 5f;
			if (dir < 0) {
				dir += 360;
			}
		} else if (Input.GetKey(KeyCode.RightArrow)) {
			started = true;
			ctrl2 = 1;
			locked = false;
			dir += 5f;
			if (dir > 360) {
				dir -= 360;
			}
		}

		if (started) {
			StartCoroutine (RecordData (ctrl1, ctrl2));
		}

		int carDir = (int)(transform.eulerAngles.y);
		if (carDir - dir > 180) {
			dir += 360;
		} else if (dir - carDir > 180) {
			carDir += 360;
		}
		if (dir > carDir + maxDir) {
			dir = carDir + maxDir;
		} else if (dir < carDir - maxDir) {
			dir = carDir - maxDir;
		}
	}

	IEnumerator RecordData(int ctrl1, int ctrl2) {
		var stepURL = baseURL + "train";

//		print (transform.eulerAngles);
		stepURL += "?srcx=" + transform.position.x;
		stepURL += "&srcy=" + transform.position.z;
		stepURL += "&dir=" + (transform.eulerAngles.y + 270) % 360;
		stepURL += "&ctrl=" + (dir + 270) % 360;

		var stepData = JsonUtility.ToJson (new Data(goalInst, obstacles, ctrl1, ctrl2));
		var req = new UnityWebRequest(stepURL, "POST");
		byte[] bodyRaw = Encoding.UTF8.GetBytes(stepData);
		req.uploadHandler = (UploadHandler) new UploadHandlerRaw(bodyRaw);
		req.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
		yield return req.SendWebRequest();
	}

	void Drive () {
		if (Mathf.Abs (transform.position.x - goalInst.transform.position.x) < 5 &&
		    Mathf.Abs (transform.position.z - goalInst.transform.position.z) < 5 && auto) {
			auto = false;
			locked = true;
			speed = 0;
			return;
		}
		Transform wheelFL = transform.Find ("Visuals/WheelFL");
		Transform wheelFR = transform.Find ("Visuals/WheelFR");
//		print (dir);
		float carDir = transform.eulerAngles.y;
		float carDeg = carDir / 180 * Mathf.PI;
		float wheelDir = dir;
		if (locked) {
			wheelDir = carDir;
		}
		wheelFL.eulerAngles = new Vector3 (0f, wheelDir, 0f);
		wheelFR.eulerAngles = new Vector3 (0f, wheelDir, 0f);
		Vector3 frontPos = transform.position + new Vector3 (Mathf.Sin (carDeg) * 3f, 0f, Mathf.Cos (carDeg) * 3f);
		Vector3 backPos = transform.position - new Vector3 (Mathf.Sin (carDeg) * 3f, 0f, Mathf.Cos (carDeg) * 3f);

		float spd = (float)speed / maxSpeed / 5f;
		float wheelDeg = (float)(wheelDir) / 180 * Mathf.PI;
		frontPos += new Vector3 (Mathf.Sin (wheelDeg) * spd, 0f, Mathf.Cos (wheelDeg) * spd);
		float tan = (frontPos.x - backPos.x) / (frontPos.z - backPos.z);
		float turn = Mathf.Atan (tan);
		if (frontPos.z - backPos.z < 0) {
			turn += Mathf.PI;
		}
		transform.eulerAngles = new Vector3(0f, turn * 180f / Mathf.PI, 0f);
		transform.position = new Vector3 ((frontPos.x + backPos.x) / 2f, 0f, (frontPos.z + backPos.z) / 2f);

		int newCarDir = (int)(transform.eulerAngles.y);
		if (newCarDir - wheelDir > 180) {
			wheelDir += 360;
		} else if (wheelDir - newCarDir > 180) {
			newCarDir += 360;
		}
		if (wheelDir - newCarDir > -10 && wheelDir - newCarDir < 10) {
			locked = true;
		}
	}
}
