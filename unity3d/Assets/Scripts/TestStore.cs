using UnityEngine;
using System.Collections;

public class TestStore : MonoBehaviour, Store.Listener {
	bool available = false;
	bool loading = false;
	bool open = false;
	string message = "";
	string purchaseToken = null;
	
	string[] skus = new string[] {
#if UNITY_ANDROID
	"android.test.purchased"
#else
	"heroes.fame_10"
#endif
	};
	
	void Start () {
		Debug.Log("Starting");
		var s = Store.Get();
		s.listener = this;
	}
	
	public void OnReady(Store.Response r) {
		Debug.Log("OnReady "+r);
		if (r.ok) {
			Debug.Log("Is available");
			available = true;
			message = "Ready";
		} else {
			Debug.Log("Is not available");
		}
	}
	
	public void OnDebug(string msg) {
		Debug.Log("Debug '"+msg+"'");
		message = "debug: "+msg;
	}
	
	public void OnInfo(Store.InfoResponse r) {
		Debug.Log("ProductInfo "+r);
		message = "info: "+r;
		loading = false;
	}
	
	public void OnPurchase(Store.PurchaseResponse r) {
		Debug.Log("OnPurchase "+r);
		
		loading = false;
		
		if (r.ok) {
			message = "purchased "+r.productSku+" with token "+r.purchaseToken;
			purchaseToken = r.purchaseToken;
		} else {
			var code = r.code;
			if (code == "canceled") {
				Debug.Log("Canceled");
				message = "purchase canceled";
			} else if (code == "empty") {
				Debug.Log("Empty");
				message = "no purchase";
			} else {
				Debug.LogWarning("OnPurcahse invalid response code "+code);
				message = "invalid response "+code;
			}
		}
	}
	
	public void OnConsume(Store.ConsumeResponse r) {
		Debug.Log("OnConsume "+r);
		
		loading = false;
		if (r.ok) {
			message = "consumed "+r.productSku+" with token "+r.purchaseToken;
			purchaseToken = null;
		} else {
			message = "failed to consume "+r.code;
		}
	}
	
	void OnGUI() {
		if (loading) return;
		
		var h = Screen.height / 10;
		
		GUILayout.BeginArea(new Rect(0f, 0f, Screen.width, Screen.height));
		
		GUILayout.Label(message);
		
		if (open) {
			if (available) {
				if (GUILayout.Button("Restore", GUILayout.Height(h))) {
					loading = true;
					Store.Get().Restore();
				}
				if (GUILayout.Button("Buy", GUILayout.Height(h))) {
					loading = true;
					Store.Get().Purchase(skus[0]);
				}
				if (purchaseToken != null) {
					if (GUILayout.Button("Consume", GUILayout.Height(h))) {
						loading = true;
						Store.Get().Consume(purchaseToken);
					}
				}
			} 
			
			if (GUILayout.Button("Close", GUILayout.Height(h))) {
				Store.Get().Close();
				open = false;
				available = false;
			}
		} else {
			if (GUILayout.Button("Open", GUILayout.Height(h))) {
				Store.Get().Initialize(skus);
				open = true;
			}
		}
		
		if (Time.timeScale == 1f) {
			if (GUILayout.Button("Pause", GUILayout.Height(h))) {
				Time.timeScale = 0f;
			}
		} else {
			if (GUILayout.Button("Unpause", GUILayout.Height(h))) {
				Time.timeScale = 1f;
			}
		}
		
		GUILayout.EndArea();
	}
}
