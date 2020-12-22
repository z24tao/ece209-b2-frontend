using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Space : MonoBehaviour {
	[System.Serializable]
	public class SpaceData {
		public float top;
		public float bot;
		public float left;
		public float right;

		public SpaceData() {
		}
	}

	public SpaceData data = new SpaceData ();

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void SetAttrs() {
		data.top = this.transform.position.z + this.transform.localScale.z / 2;
		data.bot = this.transform.position.z - this.transform.localScale.z / 2;
		data.left = this.transform.position.x - this.transform.localScale.x / 2;
		data.right = this.transform.position.x + this.transform.localScale.x / 2;
	}
}
