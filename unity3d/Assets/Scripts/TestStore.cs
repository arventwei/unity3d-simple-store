using UnityEngine;
using System.Collections;

public class TestStore : MonoBehaviour, StoreListener {
	bool available = false;
	bool allowBuy = false;
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
		
		var r = Store.ParseResponse(json);
		if (r.ok) allowBuy = true;
	}
	
	public void OnPurchase(string json) {
		Debug.Log("OnPurchase "+json);
		
		loading = false;
		
		var r = Store.ParseResponse(json);
		if (r.ok) {
			purchaseToken = (string) r.data["purchaseToken"];
		} else {
			var code = r.code;
			if (code == 7) {
				Debug.Log("Forcing Restore");
				Store.Restore();
				loading = true;
			} else if (code == 66) {
				Debug.Log("Empty");
				loading = true;
			}
		}
	}
	
	public void OnConsume(string json) {
		Debug.Log("OnConsume "+json);
		
		loading = false;
		var r = Store.ParseResponse(json);
		if (r.ok) {
			allowBuy = true;
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
		}
		if (allowBuy) {
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
