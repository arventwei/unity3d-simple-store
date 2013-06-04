using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

interface StoreDefinition {
	void Initialize();
	void Purchase(string sku);
	void Restore();
	void Consume(string token);
	void Close();
}

public class Store : MonoBehaviour, StoreDefinition {
	public class Response {
		public bool ok;
		public string error;
		public string code;
		public Hashtable data;
		
		public override string ToString() {
			return string.Format("ok: {0}, error: {1}, code: {2}, data: {3}", ""+ok, ""+error, ""+code, ""+data);
		}
	}
	
	public class PurchaseResponse : Response {
		public string purchaseToken;
		public string productSku;
		
		public override string ToString() {
			return base.ToString() + string.Format(" purchaseToken: {0}, productSku: {1}", purchaseToken, productSku);
		}
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
	public System.Action<PurchaseResponse> onPurchase = delegate {};
	public System.Action<Response> onConsume = delegate {};
	
	private bool started = false;
	
#if UNITY_EDITOR || UNITY_IPHONE
	/* 
	 * Fake implementation to allow to test all assyncronous in unity editor
	 */
	
	private bool hasPurchase = false;
	private string purchaseSku = null;
	
	private IEnumerator Latency() {
		yield return new WaitForSeconds(Random.value * 2);
	}
	
	public void Initialize() {
		StartCoroutine(FakeInitialize());
	}
	
	private IEnumerator FakeInitialize() {
		yield return StartCoroutine(Latency());
		var r = new Response();
		r.ok = Random.value > 0.1;
		if (!r.ok) {
			if (debug) Debug.Log("FakeStore.Initialize simulating fail");
			r.code = "failed";
		}
		if (debug) Debug.Log("FakeStore.Initialize");
		onReady(r);
	}
	
	public void GetInfo(string sku) {
		StartCoroutine(FakeGetInfo(sku));
	}
	
	IEnumerator FakeGetInfo(string sku) {
		yield return StartCoroutine(Latency());
		var r = new Response();
		r.ok = Random.value > 0.1;
		if (!r.ok) {
			if (debug) Debug.Log("FakeStore.GetInfo simulating fail");
			r.code = "failed";
		}
		if (debug) Debug.Log("FakeStore.GetInfo");
		onInfo(r);
	}
	
	public void Purchase(string sku) {
		StartCoroutine(FakePurchase(sku));
	}
	
	IEnumerator FakePurchase(string sku) {
		yield return StartCoroutine(Latency());
		var r = new PurchaseResponse();
		r.ok = Random.value > 0.1;
		if (!r.ok) {
			if (debug) Debug.Log("FakeStore.Purchase simulating fail");
			r.code = "failed";
		} else {
			r.purchaseToken = "123";
			r.productSku = sku;
		
			hasPurchase = true;
			purchaseSku = sku;
		}
		if (debug) Debug.Log("FakeStore.Purchase");
		onPurchase(r);
	}
	
	public void Restore() {
		StartCoroutine(FakeRestore());
	}
	
	IEnumerator FakeRestore() {
		yield return StartCoroutine(Latency());
		var r = new PurchaseResponse();
		if (hasPurchase) {
			r.ok = true;
			r.purchaseToken = "123";
			r.productSku = purchaseSku;
		} else {
			r.ok = false;
			r.code = "empty";
		}
		onPurchase(r);
	}
	
	public void Consume(string token) {
		StartCoroutine(FakeConsume(token));
	}
	
	IEnumerator FakeConsume(string token) {
		yield return StartCoroutine(Latency());
		var r = new Response();
		r.ok = Random.value > 0.1;
		if (!r.ok) {
			if (debug) Debug.Log("FakeStore.Consume simulating fail");
			r.code = "failed";
		}
		if (debug) Debug.Log("FakeStore.Consume");
		onConsume(r);
	}

	public void Close() {
		// ok 
	}
#else 
	/**
	 * Android impl
	 */
	
	private Response Parse(string json) {
		Hashtable map = (Hashtable) JSON.JsonDecode(json);
		Response r = new Response();
		Parse(r, map);
		return r;
	}
	
	private PurchaseResponse ParsePurchase(string json) {
		Hashtable map = (Hashtable) JSON.JsonDecode(json);
		PurchaseResponse r = new PurchaseResponse();
		Parse(r, map);
		if (r.data != null) {
			r.purchaseToken = (string) r.data["purchaseToken"];
			r.productSku = (string) r.data["productId"];
		}
		return r;
	}
	
	private void Parse(Response r, Hashtable map) {
		r.ok = (bool) map["ok"];
		if (map.ContainsKey("error")) 
			r.error = (string) map["error"];
		if (map.ContainsKey("code")) 
			r.code = (string) map["code"];
		if (map.ContainsKey("data")) 
			r.data = (Hashtable) map["data"];
	}
	
	
	public void Initialize() {
		AndroidJNIHelper.debug = true;
		using(AndroidJavaClass cls = new AndroidJavaClass("sisso.store.StoreService")) {
			cls.CallStatic("initialize", gameObject.name);
		}		
		started = true;
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
		started = false;
	}
	
	void OnDestroy() {
		if (started) Close();
	}
	
	void OnReady(string json) {
		if (debug) Debug.Log("OnReady "+json);
		var r = Parse(json);
		onReady(r);
	}
	
	void OnDebug(string msg) {
		onDebug(msg);
	}
	
	void OnInfo(string json) {
		if (debug) Debug.Log("OnReady "+json);
		var r = Parse(json);
		onInfo(r);
	}
	
	void OnPurchase(string json) {
		if (debug) Debug.Log("OnReady "+json);
		var r = ParsePurchase(json);
		onPurchase(r);
	}
	
	void OnConsume(string json) {
		if (debug) Debug.Log("OnReady "+json);
		var r = Parse(json);
		onConsume(r);
	}
#endif
}