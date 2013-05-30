using UnityEngine;
using System.Collections;

public interface StoreListener {
	void OnReady(string json);
	
	void OnDebug(string msg);
	
	void OnInfo(string json);
	
	void OnPurchase(string json);
	
	void OnConsume(string json);
}
