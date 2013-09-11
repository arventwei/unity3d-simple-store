using UnityEngine;
using System.Collections;

public class TestStore : MonoBehaviour {
	bool available = false;
	bool loading = false;
	
	string[] skus = new string[] {
#if UNITY_ANDROID
	"android.test.purchased"
#else
	"heroes.fame_10"
#endif
	};
	
	private string purchaseToken;
	
	void Start () {
		Debug.Log("Starting");
		var s = Store.Get();
		s.onInfo += OnInfo;
		s.onReady += OnReady;
		s.onPurchase += OnPurchase;
		s.onConsume += OnConsume;
		s.Initialize(skus);
	}
	
//	void OnDestroy() {
//		Debug.Log("Closing");
//		var s = Store.Get();
//		s.Close();
//		s.onInfo -= OnInfo;
//		s.onReady -= OnReady;
//		s.onPurchase -= OnPurchase;
//		s.onConsume -= OnConsume;
//	}
	
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
	
	public void OnPurchase(Store.PurchaseResponse r) {
		Debug.Log("OnPurchase "+r);
		
		loading = false;
		
		if (r.ok) {
			purchaseToken = (string) r.purchaseToken;
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
				loading = true;
				Store.Get().GetInfo(skus[0]);
			}
			if (GUI.Button(new Rect(100,0, 100, 100), "Restore")) {
				loading = true;
				Store.Get().Restore();
			}
			if (GUI.Button(new Rect(0, 100, 100, 100), "Buy")) {
				loading = true;
				Store.Get().Purchase(skus[0]);
			}
		}
		if (purchaseToken != null && purchaseToken.Length > 0) {
			if (GUI.Button(new Rect(100, 100, 100, 100), "Consume")) {
				loading = true;
				Store.Get().Consume(purchaseToken);
			}
		}
		
		if (Time.timeScale == 1f) {
			if (GUI.Button(new Rect(0, 200, 100, 100), "Pause")) {
				Time.timeScale = 0f;
			}
		} else {
			if (GUI.Button(new Rect(0, 200, 100, 100), "Unpause")) {
				Time.timeScale = 1f;
			}
		}
	}
}
