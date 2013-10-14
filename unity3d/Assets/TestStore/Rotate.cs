using UnityEngine;
using System.Collections;

public class Rotate : MonoBehaviour {
	void Update () {
		transform.RotateAround(Vector3.up, Time.deltaTime);
	}
}
