using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class Store {
	public class Response {
		public bool ok;
		public string error;
		// TODO change code to a string
		public string code;
		public Hashtable data;
	}
	
	public static Response ParseResponse(string json) {
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
	
	public static void Initialize(string eventListener) {
		AndroidJNIHelper.debug = true;
		using(AndroidJavaClass cls = new AndroidJavaClass("sisso.store.StoreService")) {
			cls.CallStatic("initialize", eventListener);
		}		
	}
	
	public static void GetInfo(string sku) {
		using(AndroidJavaClass cls = new AndroidJavaClass("sisso.store.StoreService")) {
			cls.CallStatic("getInfo", sku);
		}		
	}
	
	public static void Purchase(string sku) {
		using(AndroidJavaClass cls = new AndroidJavaClass("sisso.store.StoreService")) {
			cls.CallStatic("purchase", sku);
		}		
	}
	
	public static void Restore() {
		using(AndroidJavaClass cls = new AndroidJavaClass("sisso.store.StoreService")) {
			cls.CallStatic("restore");
		}		
	}
	
	public static void Consume(string token) {
		using(AndroidJavaClass cls = new AndroidJavaClass("sisso.store.StoreService")) {
			cls.CallStatic("consume", token);
		}		
	}

	public static void Close() {
		using(AndroidJavaClass cls = new AndroidJavaClass("sisso.store.StoreService")) {
			cls.CallStatic("close");
		}		
	}
}