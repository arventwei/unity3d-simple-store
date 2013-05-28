using UnityEngine;
using System.Collections;

public class TestStore : MonoBehaviour, StoreListener {
	bool available = false;
	bool allowBuy = false;
	bool allowRestore = false;
	bool allowConfirm = false;
	bool loading = false;
	
	void Start () {
		Debug.Log("Starting");
		Store.Initialize(gameObject.name);
		Debug.Log("Done");
	}
	
	public void OnReady() {
		Debug.Log("OnReady");
		
		available = Store.IsAvailable();
		if (Store.IsAvailable()) {
			Debug.Log("Is available");
		} else {
			Debug.Log("Is not available");
		}
	}
	
	public void OnDebug(string msg) {
		Debug.Log("Debug '"+msg+"'");
	}
	
	public void OnInfo(string json) {
		Debug.Log("ProductInfo "+json);
		
		loading = false;
		allowBuy = true;
	}
	
	public void OnPurchase(string json) {
		Debug.Log("OnPurchase "+json);
		
		loading = false;
		allowConfirm = true;
	}
	
	public void OnConsume(string json) {
		Debug.Log("OnConsume "+json);
		
		loading = false;
		allowBuy = true;
	}
	
	void OnGUI() {
		if (loading) return;
		
		if (available) {
			if (GUI.Button(new Rect(0,0, 100, 100), "GetInfo")) {
				Store.GetInfo("android.test.purchased");
				loading = true;
			}
		}
		if (allowBuy) {
			if (GUI.Button(new Rect(100,0, 100, 100), "Buy")) {
				Store.Purchase("android.test.purchased");
				loading = true;
			}
		}
	}
}
