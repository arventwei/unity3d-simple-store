package sisso.store;

import java.util.ArrayList;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import android.app.Activity;
import android.app.PendingIntent;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.ServiceConnection;
import android.graphics.Bitmap.Config;
import android.os.Bundle;
import android.os.IBinder;
import android.util.Log;

import com.android.vending.billing.IInAppBillingService;
import com.unity3d.player.UnityPlayer;

/*
 * initialize OnReady
 * getInfo OnInfo
 * purchase OnPurchase
 * restore OnPurchase
 * consume OnConsume
 */
public class StoreService implements ServiceConnection {
	private static final String TAG = "StoreService";
	private static StoreService instance;

	private IInAppBillingService service;
	private String listener;
	private String[] skus;

	public static StoreService get() {
		if (instance == null) instance = new StoreService();
		return instance;
	}

	public static void initialize(String listenerName, String jsonSkus) {
		Log.d(TAG, "initialize start");
		Log.d(TAG, "listener: "+listenerName);
		Log.d(TAG, "skus: "+jsonSkus);
		
		StoreService service = get();
		service.listener = listenerName;
		
		String[] skus = new String[0];
		
		try {
			JSONArray json = new JSONArray(jsonSkus);
			skus = new String[json.length()];
			for (int i = 0; i < skus.length; i++) {
				skus[i] = json.getString(i);
			}
		} catch (Exception e) {
			e.printStackTrace();
		}
		
		service.skus = skus;
		
		UnityPlayer.currentActivity.bindService(new Intent(Cons.IAP_BIND), service, Context.BIND_AUTO_CREATE);
		Log.d(TAG, "initialize finished");
	}

	private static void debug(String message) {
		Log.d(TAG, message);
		sendMessage("OnDebug", message);
	}
	
	public static void close() {
		Log.d(TAG, "close");
		UnityPlayer.currentActivity.unbindService(get());
	}

	private static void run(Runnable runnnable) {
		Thread thread = new Thread(runnnable);
		thread.setDaemon(true);
		thread.start();
	}
	
	public static void getInfo(final String... skus) {
		run(new Runnable() {
			@Override
			public void run() {
				try {
					Log.d(TAG, "getInfo thread started");

					ArrayList skuList = new ArrayList();
					for (String sku : skus)
						skuList.add(sku);
					Bundle querySkus = new Bundle();
					querySkus.putStringArrayList("ITEM_ID_LIST", skuList);
	
					Bundle skuDetails = instance.service.getSkuDetails(3,
							UnityPlayer.currentActivity.getPackageName(), "inapp",
							querySkus);

					int response = skuDetails.getInt("RESPONSE_CODE");
					if (response != Cons.RESULT_OK) {
						sendMessage(Cons.EVENT_ONINFO, buildError("Invalid response code: "+response, Cons.MESSAGE_CODE_FAILED));
						return;
					}

					ArrayList<String> responseList = skuDetails
							.getStringArrayList("DETAILS_LIST");
					for (String thisResponse : responseList) {
						sendMessage(Cons.EVENT_ONINFO, buildResult(new JSONObject(thisResponse)));
					}
					Log.d(TAG, "getInfo thread finished");
				} catch (Exception e) {
					e.printStackTrace();
					sendMessage(Cons.EVENT_ONINFO, buildError(e.getMessage(), Cons.MESSAGE_CODE_EXCEPTION));
				}
			}
		});
	}

	public static void purchase(final String sku) {
		Log.d(TAG, "purchase started for '"+sku+"'");
		try {
			Bundle buyIntentBundle = get().service.getBuyIntent(3, UnityPlayer.currentActivity.getPackageName(), sku, "inapp", "");
			int response = buyIntentBundle.getInt("RESPONSE_CODE");
			if (response == Cons.RESULT_OK) {
				PendingIntent pendingIntent = buyIntentBundle.getParcelable("BUY_INTENT");
				UnityPlayer.currentActivity.startIntentSenderForResult(
						pendingIntent.getIntentSender(), Cons.REQUEST_CODE_PURCHASE,
						new Intent(), Integer.valueOf(0),
						Integer.valueOf(0), Integer.valueOf(0));
			} else if (response == Cons.RESULT_OWNED) {
				Log.d(TAG, "purchase thread product already owned, restoring");
				restore();
			} else {
				sendMessage(Cons.EVENT_ONPURCHASE, buildError("Invalid response code: "+response, Cons.MESSAGE_CODE_FAILED));
			}
		} catch (final Exception e) {
			e.printStackTrace();
			sendMessage(Cons.EVENT_ONPURCHASE, buildError(e.getMessage(), Cons.MESSAGE_CODE_EXCEPTION));
		}
		Log.d(TAG, "purchase finished");
	}

	public static void consume(final String purchaseToken) {
		try {
			Log.d(TAG, "consume thread started for '"+purchaseToken+"'");
			int response = get().service.consumePurchase(3, UnityPlayer.currentActivity.getPackageName(), purchaseToken);
			if (response == Cons.RESULT_OK) {
				JSONObject result = new JSONObject();
				result.put("token", purchaseToken);
				sendMessage(Cons.EVENT_ONCONSUME, buildResult(result));
			} else {
				sendMessage(Cons.EVENT_ONCONSUME, buildError("Invalid response code: "+response, Cons.MESSAGE_CODE_FAILED));
			}
			Log.d(TAG, "consume thread finish");
		} catch (final Exception e) {
			e.printStackTrace();
			sendMessage(Cons.EVENT_ONCONSUME, buildError(e.getMessage(), Cons.MESSAGE_CODE_EXCEPTION));
		}
	}

	public static void restore() {
		run(new Runnable() {
			@Override
			public void run() {
				try {
					Log.d(TAG, "restore thread started");
					
					Bundle ownedItems = get().service.getPurchases(3, UnityPlayer.currentActivity.getPackageName(), "inapp", null);
					int response = ownedItems.getInt("RESPONSE_CODE");
					if (response == Cons.RESULT_OK) {
						ArrayList data = ownedItems.getStringArrayList("INAPP_PURCHASE_DATA_LIST");
						for (int i = 0; i < data.size(); ++i) {
							sendMessage(Cons.EVENT_ONPURCHASE, buildResult(new JSONObject((String) data.get(i))));
						}
						
						if (data.size() == 0) {
							sendMessage(Cons.EVENT_ONPURCHASE, buildError("There is no purchase to restore", Cons.MESSAGE_CODE_EMPTY));
						}
					} else {
						sendMessage(Cons.EVENT_ONPURCHASE, buildError("Invalid response: "+response, Cons.MESSAGE_CODE_FAILED));
					}
					Log.d(TAG, "restore thread finished");
				} catch (final Exception e) {
					e.printStackTrace();
					sendMessage(Cons.EVENT_ONPURCHASE, buildError(e.getMessage(), Cons.MESSAGE_CODE_EXCEPTION));
				}
			}
		});
	}

	private void checkAvailability() {
		Log.d(TAG, "checkAvailability started");
		try {
			boolean available = service.isBillingSupported(3, UnityPlayer.currentActivity.getPackageName(), Cons.INAPP) == Cons.RESULT_OK;
			sendMessage(Cons.EVENT_ONREADY, available ? buildResult(null) : buildError("not available", Cons.MESSAGE_CODE_FAILED));
			// automatically check for products
			getInfo(skus);
		} catch (Exception e) {
			e.printStackTrace();
			sendMessage(Cons.EVENT_ONREADY, buildError("Exception "+e.getMessage(), Cons.MESSAGE_CODE_FAILED));
		}
		Log.d(TAG, "checkAvailability finished");
	}

	private static String buildError(String msg, String code) {
		try {
			JSONObject map = new JSONObject();
			map.put("ok", false);
			map.put("error", msg);
			map.put("code", code);
			return map.toString();
		} catch(Exception e) {
			return "failed to generate error json with msg '"+msg+"'";
		}
	}
	
	private static String buildResult(JSONObject obj) {
		try {
			JSONObject map = new JSONObject();
			map.put("ok", true);
			map.put("data", obj);
			return map.toString();
		} catch(Exception e) {
			return "failed to generate response msg for "+obj.toString();
		}
	}

	@Override
	public void onServiceConnected(ComponentName name, IBinder service) {
		this.service = IInAppBillingService.Stub.asInterface(service);
		checkAvailability();
	}

	private static void sendMessage(String message, String value) {
		UnityPlayer.UnitySendMessage(get().listener, message, value);
	}

	@Override
	public void onServiceDisconnected(ComponentName name) {
		this.service = null;
		sendMessage(Cons.EVENT_ONREADY, buildError("On service disconnected", Cons.MESSAGE_CODE_FAILED));
	}
	
	public void onActivityResult(int requestCode, int resultCode, Intent data) {
		Log.d(TAG, "onActivityResult start request code "+requestCode+" result code "+resultCode);
		
		try {
			if (requestCode == Cons.REQUEST_CODE_PURCHASE) {
				if (resultCode == Activity.RESULT_OK) {
					int responseCode = data.getIntExtra("RESPONSE_CODE", -1);
					String purchaseData = data.getStringExtra("INAPP_PURCHASE_DATA");
					if (responseCode == Cons.RESULT_OK) {
						if (purchaseData == null) {
							Log.d(TAG, "onActivityResult purchase is ok but data is null!");
							sendMessage(Cons.EVENT_ONPURCHASE, buildError("Purchase return a empty data", Cons.MESSAGE_CODE_FAILED));
						} else {
							Log.d(TAG, "onActivityResult purchase ok");
							sendMessage(Cons.EVENT_ONPURCHASE, buildResult(new JSONObject(purchaseData)));
						}
					} else if (responseCode == Cons.RESULT_CANCELED) {
						Log.d(TAG, "onActivityResult purchase activity canceled");
						sendMessage(Cons.EVENT_ONPURCHASE, buildError("The purchase was canceled", Cons.MESSAGE_CODE_CANCELED));
					} else {
						Log.d(TAG, "onActivityResult purchase unknown "+responseCode);
						sendMessage(Cons.EVENT_ONPURCHASE, buildError("Invalid response code "+responseCode, Cons.MESSAGE_CODE_FAILED));
					}
				} else {
					Log.d(TAG, "onActivityResult invalid activity result code "+resultCode);
					sendMessage(Cons.EVENT_ONPURCHASE, buildError("onActivityResult invalid activity result code "+resultCode, Cons.MESSAGE_CODE_FAILED));
				}
			} else {
				debug("onActivityResult invalid code");
			}
		} catch (Exception e) {
			e.printStackTrace();
			debug(e.getMessage());
			sendMessage(Cons.EVENT_ONPURCHASE, buildError("Exception: "+e.getMessage(), Cons.MESSAGE_CODE_EXCEPTION));
		}
	}
}
