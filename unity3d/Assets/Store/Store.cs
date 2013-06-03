using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class Store : MonoBehaviour {
	public class Response {
		public bool ok;
		public string error;
		public string code;
		public Hashtable data;
		
		public string ToString() {
			return string.Format("{ok: {0}, error: {1}, code: {2}, data: {3}", ""+ok, ""+error, ""+code, ""+data);
		}
	}
	
	private Response ParseResponse(string json) {
		Hashtable map = (Hashtable) JSON.JsonDecode(json);
		Response r = new Response();
		r.ok = (bool) map["ok"];
		if (map.ContainsKey("error")) 
			r.error = (string) map["error"];
		if (map.ContainsKey("code")) 
			r.code = (string) map["code"];
		if (map.ContainsKey("data")) 
			r.data = (Hashtable) map["data"];
		return r;
	}
	
	private static Store instance;
	
	public static Store Get() {
		if (instance == null) {
			var obj = new GameObject(typeof(Store).Name);
			instance = obj.AddComponent<Store>();
			GameObject.DontDestroyOnLoad(obj);
		}
		
		return instance;
	}
	
	public bool debug = true;
	
	public System.Action<Response> onReady = delegate {};
	public System.Action<string> onDebug = delegate {};
	public System.Action<Response> onInfo = delegate {};
	public System.Action<Response> onPurchase = delegate {};
	public System.Action<Response> onConsume = delegate {};
	
	public void Initialize() {
		AndroidJNIHelper.debug = true;
		using(AndroidJavaClass cls = new AndroidJavaClass("sisso.store.StoreService")) {
			cls.CallStatic("initialize", gameObject.name);
		}		
	}
	
	public void GetInfo(string sku) {
		using(AndroidJavaClass cls = new AndroidJavaClass("sisso.store.StoreService")) {
			cls.CallStatic("getInfo", sku);
		}		
	}
	
	public void Purchase(string sku) {
		using(AndroidJavaClass cls = new AndroidJavaClass("sisso.store.StoreService")) {
			cls.CallStatic("purchase", sku);
		}		
	}
	
	public void Restore() {
		using(AndroidJavaClass cls = new AndroidJavaClass("sisso.store.StoreService")) {
			cls.CallStatic("restore");
		}		
	}
	
	public void Consume(string token) {
		using(AndroidJavaClass cls = new AndroidJavaClass("sisso.store.StoreService")) {
			cls.CallStatic("consume", token);
		}		
	}

	public void Close() {
		using(AndroidJavaClass cls = new AndroidJavaClass("sisso.store.StoreService")) {
			cls.CallStatic("close");
		}		
	}
	
	void OnReady(string json) {
		if (debug) Debug.Log("OnReady "+json);
		var r = ParseResponse(json);
		onReady(r);
	}
	
	void OnDebug(string msg) {
		onDebug(msg);
	}
	
	void OnInfo(string json) {
		if (debug) Debug.Log("OnReady "+json);
		var r = ParseResponse(json);
		onInfo(r);
	}
	
	void OnPurchase(string json) {
		if (debug) Debug.Log("OnReady "+json);
		var r = ParseResponse(json);
		onPurchase(r);
	}
	
	void OnConsume(string json) {
		if (debug) Debug.Log("OnReady "+json);
		var r = ParseResponse(json);
		onConsume(r);
	}
}