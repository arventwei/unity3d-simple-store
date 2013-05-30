using UnityEngine;
using System.Collections;

public class TestStore : MonoBehaviour, StoreListener {
	bool available = false;
	bool loading = false;
	
	private string purchaseToken;
	
	void Start () {
		Debug.Log("Starting");
		Store.Initialize(gameObject.name);
		Debug.Log("Done");
	}
	
	void OnDestroy() {
		Store.Close();
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
	}
	
	public void OnPurchase(string json) {
		Debug.Log("OnPurchase "+json);
		
		loading = false;
		
		var r = Store.ParseResponse(json);
		if (r.ok) {
			purchaseToken = (string) r.data["purchaseToken"];
		} else {
			var code = r.code;
			if (code == "canceled") {
				Debug.Log("Canceled");
				loading = true;
			} else if (code == "empty") {
				Debug.Log("Empty");
				loading = true;
			} else {
				Debug.LogWarning("OnPurcahse invalid response code "+code);
			}
		}
	}
	
	public void OnConsume(string json) {
		Debug.Log("OnConsume "+json);
		
		loading = false;
		var r = Store.ParseResponse(json);
		if (r.ok) {
			purchaseToken = null;
		}
	}
	
	void OnGUI() {
		if (loading) return;
		
		if (available) {
			if (GUI.Button(new Rect(0,0, 100, 100), "Get")) {
				Store.GetInfo("android.test.purchased");
				loading = true;
			}
			if (GUI.Button(new Rect(100,0, 100, 100), "Restore")) {
				Store.Restore();
				loading = true;
			}
			if (GUI.Button(new Rect(0, 100, 100, 100), "Buy")) {
				Store.Purchase("android.test.purchased");
				loading = true;
			}
		}
		if (purchaseToken != null && purchaseToken.Length > 0) {
			if (GUI.Button(new Rect(100, 100, 100, 100), "Consume")) {
				Store.Consume(purchaseToken);
				loading = true;
			}
		}
	}
}
