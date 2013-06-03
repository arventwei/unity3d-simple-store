using UnityEngine;
using System.Collections;

public class TestStore : MonoBehaviour {
	bool available = false;
	bool loading = false;
	
	private string purchaseToken;
	
	void Start () {
		Debug.Log("Starting");
		var s = Store.Get();
		s.onInfo += OnInfo;
		s.onReady += OnReady;
		s.onPurchase += OnPurchase;
		s.onConsume += OnConsume;
		s.Initialize();
	}
	
	void OnDestroy() {
		Debug.Log("Closing");
		var s = Store.Get();
		s.onInfo -= OnInfo;
		s.onReady -= OnReady;
		s.onPurchase -= OnPurchase;
		s.onConsume -= OnConsume;
		s.Close();
	}
	
	public void OnReady(Store.Response r) {
		Debug.Log("OnReady "+r);
		if (r.ok) {
			Debug.Log("Is available");
			available = true;
		} else {
			Debug.Log("Is not available");
		}
	}
	
	public void OnDebug(string msg) {
		Debug.Log("Debug '"+msg+"'");
	}
	
	public void OnInfo(Store.Response r) {
		Debug.Log("ProductInfo "+r);
		
		loading = false;
	}
	
	public void OnPurchase(Store.Response r) {
		Debug.Log("OnPurchase "+r);
		
		loading = false;
		
		if (r.ok) {
			purchaseToken = (string) r.data["purchaseToken"];
		} else {
			var code = r.code;
			if (code == "canceled") {
				Debug.Log("Canceled");
			} else if (code == "empty") {
				Debug.Log("Empty");
			} else {
				Debug.LogWarning("OnPurcahse invalid response code "+code);
			}
		}
	}
	
	public void OnConsume(Store.Response r) {
		Debug.Log("OnConsume "+r);
		
		loading = false;
		if (r.ok) {
			purchaseToken = null;
		}
	}
	
	void OnGUI() {
		if (loading) return;
		
		if (available) {
			if (GUI.Button(new Rect(0,0, 100, 100), "Get")) {
				Store.Get().GetInfo("android.test.purchased");
				loading = true;
			}
			if (GUI.Button(new Rect(100,0, 100, 100), "Restore")) {
				Store.Get().Restore();
				loading = true;
			}
			if (GUI.Button(new Rect(0, 100, 100, 100), "Buy")) {
				Store.Get().Purchase("android.test.purchased");
				loading = true;
			}
		}
		if (purchaseToken != null && purchaseToken.Length > 0) {
			if (GUI.Button(new Rect(100, 100, 100, 100), "Consume")) {
				Store.Get().Consume(purchaseToken);
				loading = true;
			}
		}
	}
}
