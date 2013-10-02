using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

interface StoreDefinition {
	void Initialize(string[] skus);
	void Purchase(string sku);
	void GetInfo(string sku);
	void Restore();
	void Consume(string token);
	void Close();
}

#if UNITY_IPHONE && ! FAKE_IAP && ! UNITY_EDITOR
class StoreBinding 
{
	[DllImport("__Internal")]
	private static extern void _initStoreWithProducts(string callbackName, string products);
	
	[DllImport("__Internal")]
	private static extern bool _canMakeStorePurchases();
	
	[DllImport("__Internal")]
	private static extern void _getProductInfo(string itemName);
	
	[DllImport("__Internal")]
	private static extern void _purchaseProduct(string itemName);
	
	[DllImport("__Internal")]
	private static extern void _setVerificationServer(string url);
	
	public static void LoadStore(string callbackName, string[] productArray)
	{
		string products = ArrayToString(productArray);
		
		_initStoreWithProducts(callbackName, products);

	}

	public static bool CanMakeStorePurchases()
	{
		return _canMakeStorePurchases();	
	}
	
	public static void LoadStoreProductsWithInfo(string[] productArray)
	{
		// Call this only after you receive notification that the store loaded properly
		// 
		foreach(string product in productArray)
		{
			_getProductInfo(product);	
		}
		
	}
	
	public static void SetReceiptVerificationServer(string url)
	{
		Debug.Log("Setting URL to " + url);
		_setVerificationServer(url);
	}
	
	public static void PurchaseProduct(string productName)
	{
		_purchaseProduct(productName);
	}
	
	public static void GetProductInfo(string productName)
	{
		_getProductInfo(productName);
	}
	
	public static string ArrayToString(string[] convertArray)
	{
		string returnString = string.Empty;
		for(int i=0; i<convertArray.Length; i++)
		{
			returnString += convertArray[i];
			if(i != convertArray.Length - 1)
			{
				returnString += "|";	
			}
		}
		
		return returnString;
	}
	
	public static StoreProduct StringToProduct(string productInfo)
	{
		string[] words = productInfo.Split('|');
		if(words.Length == 4)
		{
			StoreProduct product = new StoreProduct(
				words[0],
				words[1],
				words[2],
				words[3]);
			return product;
		}else{
			throw new System.FormatException("Could NOT create Product from string " + productInfo);	
		}
	}
}


public class StoreProduct
{
	private string _title;
	private string _description;
	private string _price;
	private string _productId;
	
	public StoreProduct(string title, string description, string price, string productId)
	{
		_title       = title;
		_description = description;
		_price       = price;
		_productId   = productId;
	}
	
	public string Title
	{
		get{ return _title; }	
	}
	
	public string Description
	{
		get{ return _description; }	
	}
	
	public string Price
	{
		get{ return _price; }	
	}
	
	public string ProductIdentifier
	{
		get{ return _productId; }	
	}
}
#endif

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
	
	public class ConsumeResponse : PurchaseResponse {
	}
	
	public class InfoResponse : Response {
		public string productTitle;
		public string productDescription;
		public string productPrice;
		public string productSku;
		
		public override string ToString() {
			return base.ToString() + string.Format(" productSku: {0}, productPrice: {1}, productTitle: {2}, productDescription: {3}", productSku, productPrice, productTitle, productDescription);
		}
	}
	
	public interface Listener {
		void OnReady(Response response);
		void OnDebug(string msg);
		void OnInfo(InfoResponse response);
		void OnPurchase(PurchaseResponse response);
		void OnConsume(ConsumeResponse response);
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
	
	public Listener listener;
	
#if UNITY_IPHONE && ! FAKE_IAP && ! UNITY_EDITOR
	List<StoreProduct> products = new List<StoreProduct>();
	string[] productIdentifiers = new string[0];

	// any value different of null will activate server verification
	string receiptServer = null;
	
	public void Initialize(string[] skus) {
		if (debug) Debug.Log("Initializing with skus "+skus);
		
		this.productIdentifiers = skus;
		
		// Make sure the user has enabled purchases in their settings before doing anything
		if(StoreBinding.CanMakeStorePurchases())
		{
			StoreBinding.LoadStore(gameObject.name, productIdentifiers);
			if(receiptServer != null)
			{
				Debug.Log("Adding Server Verification " + receiptServer);
				StoreBinding.SetReceiptVerificationServer(receiptServer.ToString());
			}
		} else {
			Response r = new Response();
			r.ok = false;
			r.code = "failed";
			r.error = "CanMakeStorePurchases return false";
			listener.OnReady(r);
		}
	}

	/** 
	 * Because the IOS api automatically consume a purchase, if was 
	 * successful it call directly OnConsume without call OnPurchase.
	 */
	public void Purchase(string sku) {
		if (debug) Debug.Log("Purchase "+sku);
		StoreBinding.PurchaseProduct(sku);
	}
	
	public void Restore() {
		if (debug) Debug.Log("Restore");
		
		var r = new PurchaseResponse();
		r.ok = false;
		r.code = "not-implemented";
		listener.OnPurchase(r);
	}
	
	public void Consume(string token) {
		if (debug) Debug.Log("Consume "+token);
		
		// they are automatically consumed in iap api
				
		var r = new ConsumeResponse();
		r.ok = false;
		r.code = "not-implemented";
		
		listener.OnConsume(r);
	}
	
	public void GetInfo(string sku) {
		if (debug) Debug.Log("GetInfo");
		
		InfoResponse r = null;
		
		foreach (var p in products) {
			if (p.ProductIdentifier == sku) {
				r = new InfoResponse();
				r.ok = true;
				r.productTitle = p.Title;
				r.productDescription = p.Description;
				r.productPrice = p.Price;
				r.productSku = p.ProductIdentifier;
				break;
			}
		}
		
		if (r == null) {
			// not found
			r = new InfoResponse();
			r.ok = false;
			r.code = "failed";
			r.error = "Could not found product with sku "+sku;
		}
		
		listener.OnInfo(r);
	}
	
	public void Close() {
		if (debug) Debug.Log("Close");
	}
	
	public void OnDebug(string msg) {
		if (debug) Debug.Log("OnDebug "+msg);
		listener.OnDebug(msg);
	}
	
	public void	CallbackStoreLoadedSuccessfully(string empty)
	{
		// Called From Objective-C when the store has successfully finished loading
		StoreBinding.LoadStoreProductsWithInfo(productIdentifiers);
		
		Response r = new Response();
		r.ok = true;
		listener.OnReady(r);
	}
	
	public void CallbackStoreLoadFailed(string empty)
	{
		Debug.Log("Store Failed to load");	
		Response r = new Response();
		r.ok = false;
		r.code = "failed";
		r.error = "Store failed to load with result: "+empty;
		listener.OnReady(r);
	}
	
	public void CallbackReceiveProductInfo(string info)
	{
		// Called From Objective-C After LoadStoreProductsWithInfo for each item
		
		StoreProduct product = StoreBinding.StringToProduct(info);
		products.Add(product);
		
		if (debug) Debug.Log("Receive product info "+info);
	}
	
	public void CallbackProvideContent(string productIdentifier)
	{
		// Called from Objective-C when a store purchase succeeded
		if (debug) Debug.Log("Purchase Succeeded " + productIdentifier);
		
		var r = new ConsumeResponse();
		r.ok = true;
		r.productSku = productIdentifier;
		r.purchaseToken = "";
		listener.OnConsume(r);
	}
	
	public void CallbackTransactionFailed(string code)
	{
		// Called from Objective-C when a transaction failed
		if (debug) Debug.LogError("Purchase Failed");

		PurchaseResponse r = new PurchaseResponse();
		r.ok = false;
		r.code = code;
		listener.OnPurchase(r);
	}
	
	public StoreProduct[] ListProducts()
	{
		// Make the List an Array to prevent Mutation outside this class
		
		StoreProduct[] productArray = new StoreProduct[products.Count];
		for(int i=0; i<products.Count; i++)
		{
			productArray[i] = products[i];
		}
		return productArray;
	}	
#elif UNITY_ANDROID && ! FAKE_IAP && ! UNITY_EDITOR
	/**
	 * Android impl
	 */
	
	private bool started = false;
	
	private Dictionary<string, string> skuByPurchase = new Dictionary<string, string>();
	
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
		
	private ConsumeResponse ParseConsume(string json) {
		Hashtable map = (Hashtable) JSON.JsonDecode(json);
		ConsumeResponse r = new ConsumeResponse();
		Parse(r, map);
		if (r.data != null) {
			r.purchaseToken = (string) r.data["purchaseToken"];
			if (!skuByPurchase.ContainsKey(r.purchaseToken)) {
				r.ok = false;
				r.code = "token-not-found";
				r.error = "Could not found purchase for token "+r.purchaseToken;
				return r;
				
			}
			r.productSku = skuByPurchase[r.purchaseToken];
		}
		return r;
	}
	
	private InfoResponse ParseInfo(string json) {
		Hashtable map = (Hashtable) JSON.JsonDecode(json);
		InfoResponse r = new InfoResponse();
		Parse(r, map);
		if (r.data != null) {
			r.productSku = (string) r.data["productId"];
			r.productPrice = (string) r.data["price"];
			r.productTitle = (string) r.data["title"];
			r.productDescription = (string) r.data["description"];
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
	
	
	public void Initialize(string[] skus) {
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
		listener.OnReady(r);
	}
	
	void OnDebug(string msg) {
		if (debug) Debug.Log("OnDebug "+msg);
		listener.OnDebug(msg);
	}
	
	void OnInfo(string json) {
		if (debug) Debug.Log("OnInfo "+json);
		var r = ParseInfo(json);
		listener.OnInfo(r);
	}
	
	void OnPurchase(string json) {
		if (debug) Debug.Log("OnPurchase "+json);
		var r = ParsePurchase(json);
		if (r.ok) skuByPurchase.Add(r.purchaseToken, r.productSku);
		listener.OnPurchase(r);
	}
	
	void OnConsume(string json) {
		if (debug) Debug.Log("OnConsume "+json);
		var r = ParseConsume(json);
		listener.OnConsume(r);
	}
#else
	/* 
	 * Fake implementation to allow to test all assyncronous in unity editor
	 */
	
	private Dictionary<string, string> skuByPurchase = new Dictionary<string, string>();
	
	private IEnumerator Latency() {
		// simulate a wait time using frames assuming that app is at target fps
		var fps = Application.targetFrameRate;
		if (fps <= 0) fps = 30;
		var framesToWait = fps * (2 * Random.value + 1);
		while (framesToWait > 0) {
			yield return new WaitForEndOfFrame();
			framesToWait --;
		}
	}
	
	public void Initialize(string[] skus) {
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
		listener.OnReady(r);
	}
	
	public void GetInfo(string sku) {
		StartCoroutine(FakeGetInfo(sku));
	}
	
	IEnumerator FakeGetInfo(string sku) {
		yield return StartCoroutine(Latency());
		var r = new InfoResponse();
		r.ok = Random.value > 0.1;
		if (r.ok) {
			r.productTitle = "Title";
			r.productPrice = "$ 0.99";
			r.productDescription = "Desc";
			r.productSku = sku;
		} else {
			if (debug) Debug.Log("FakeStore.GetInfo simulating fail");
			r.code = "failed";
		}
		if (debug) Debug.Log("FakeStore.GetInfo");
		listener.OnInfo(r);
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
			r.purchaseToken = Random.Range(0, 9999).ToString("0000");
			r.productSku = sku;
		
			skuByPurchase.Add(r.purchaseToken, sku);
		}
		if (debug) Debug.Log("FakeStore.Purchase");
		listener.OnPurchase(r);
	}
	
	public void Restore() {
		StartCoroutine(FakeRestore());
	}
	
	IEnumerator FakeRestore() {
		yield return StartCoroutine(Latency());
		foreach (var key in skuByPurchase.Keys) {
			var r = new PurchaseResponse();
			r.ok = true;
			r.purchaseToken = skuByPurchase[key];
			r.productSku = key;
			listener.OnPurchase(r);
		} 
		
		if (skuByPurchase.Count == 0) {
			var r = new PurchaseResponse();
			r.ok = false;
			r.code = "empty";
			listener.OnPurchase(r);
		}
	}
	
	public void Consume(string token) {
		StartCoroutine(FakeConsume(token));
	}
	
	IEnumerator FakeConsume(string token) {
		yield return StartCoroutine(Latency());
		var r = new ConsumeResponse();
		r.ok = Random.value > 0.1 && skuByPurchase.ContainsKey(token);
		if (r.ok) {
			r.productSku = skuByPurchase[token];
			skuByPurchase.Remove(token);
		} else {
			if (debug) Debug.Log("FakeStore.Consume simulating fail");
			r.code = "failed";
		}
		if (debug) Debug.Log("FakeStore.Consume");
		r.purchaseToken = token;
		listener.OnConsume(r);
	}
	
	void OnDebug(string msg) {
		if (debug) Debug.Log("OnDebug "+msg);
		listener.OnDebug(msg);
	}
	
	public void Close() {
		// ok 
	}
#endif
}
