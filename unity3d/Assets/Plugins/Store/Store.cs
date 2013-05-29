using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class Store {
	public static void Initialize(string eventListener) {
		AndroidJNIHelper.debug = true;
		using(AndroidJavaClass cls = new AndroidJavaClass("sisso.store.StoreService")) {
			cls.CallStatic("initialize", eventListener);
		}		
	}
	
	public static bool IsAvailable() {
		using(AndroidJavaClass cls = new AndroidJavaClass("sisso.store.StoreService")) {
			return cls.CallStatic<bool>("isAvailable");
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
}