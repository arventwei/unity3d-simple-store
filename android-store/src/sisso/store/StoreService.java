package sisso.store;

import java.util.ArrayList;

import org.json.JSONException;
import org.json.JSONObject;

import android.app.Activity;
import android.app.PendingIntent;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.ServiceConnection;
import android.os.Bundle;
import android.os.IBinder;
import android.util.Log;

import com.android.vending.billing.IInAppBillingService;
import com.unity3d.player.UnityPlayer;

public class StoreService implements ServiceConnection {
	private static final String TAG = "StoreService";
	private static StoreService instance;

	private IInAppBillingService service;
	private String listener;

	public static StoreService get() {
		if (instance == null) instance = new StoreService();
		return instance;
	}

	public static void initialize(String listenerName) {
		get().listener = listenerName;

		debug("iniatialize start");
		UnityPlayer.currentActivity.bindService(new Intent(Cons.IAP_BIND),
				get(), Context.BIND_AUTO_CREATE);
	}

	private static void debug(String message) {
		Log.d(TAG, message);
		sendMessage("OnDebug", message);
	}
	
	public static void close() {

	}

	public static void getInfo(final String sku) {
		Log.d(TAG, "getInfo started");

		Thread thread = new Thread() {
			@Override
			public void run() {
				try {
					Log.d(TAG, "getInfo thread started");

					ArrayList skuList = new ArrayList();
					skuList.add(sku);
					Bundle querySkus = new Bundle();
					querySkus.putStringArrayList("ITEM_ID_LIST", skuList);
	
					Bundle skuDetails = instance.service.getSkuDetails(3,
							UnityPlayer.currentActivity.getPackageName(), "inapp",
							querySkus);

					int response = skuDetails.getInt("RESPONSE_CODE");
					if (response != Cons.RESULT_OK) {
						sendMessage(Cons.EVENT_ONINFO, buildError("Invalid response code: "+response, response));
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
					sendMessage(Cons.EVENT_ONINFO, buildError(e.getMessage(), null));
				}
			}
		};
		thread.setDaemon(true);
		thread.start();
		
		Log.d(TAG, "getInfo finished");
	}

	public static void purchase(final String sku) {
		Log.d(TAG, "purchase started");

		Thread thread = new Thread() {
			@Override
			public void run() {
				try {
					Log.d(TAG, "purchase thread started");

					Bundle buyIntentBundle = get().service.getBuyIntent(3, UnityPlayer.currentActivity.getPackageName(), sku, "inapp", "random-value");
					int response = buyIntentBundle.getInt("RESPONSE_CODE");
					if (response == Cons.RESULT_OK) {
						PendingIntent pendingIntent = buyIntentBundle.getParcelable("BUY_INTENT");
						UnityPlayer.currentActivity.startIntentSenderForResult(
								pendingIntent.getIntentSender(), Cons.REQUEST_CODE_PURCHASE,
								new Intent(), Integer.valueOf(0),
								Integer.valueOf(0), Integer.valueOf(0));
					} else {
						sendMessage(Cons.EVENT_ONPURCHASE, buildError("Invalid response code: "+response, response));
					}
					Log.d(TAG, "purchase thread finish");
				} catch (final Exception e) {
					e.printStackTrace();
					sendMessage(Cons.EVENT_ONPURCHASE, buildError(e.getMessage(), null));
				}
			}
		};
		thread.setDaemon(true);
		thread.start();

		Log.d(TAG, "purchase finished");
	}

	public static void consume(final String purchaseToken) {
		Log.d(TAG, "consume started");

		Thread thread = new Thread() {
			@Override
			public void run() {
				try {
					Log.d(TAG, "consume thread started");
					int response = get().service.consumePurchase(3, UnityPlayer.currentActivity.getPackageName(), purchaseToken);
					if (response == Cons.RESULT_OK) {
						sendMessage(Cons.EVENT_ONCONSUME, buildResult(null));
					} else {
						sendMessage(Cons.EVENT_ONCONSUME, buildError("Invalid response code: "+response, response));
					}
					Log.d(TAG, "consume thread finish");
				} catch (final Exception e) {
					e.printStackTrace();
					sendMessage(Cons.EVENT_ONCONSUME, buildError(e.getMessage(), null));
				}
			}
		};
		thread.setDaemon(true);
		thread.start();

		Log.d(TAG, "consume finished");
	}

	public static void restore() {
		Log.d(TAG, "restore started");
		Thread thread = new Thread() {
			@Override
			public void run() {
				try {
					Log.d(TAG, "restore thread started");
					
					Bundle ownedItems = get().service.getPurchases(3, UnityPlayer.currentActivity.getPackageName(), "inapp", null);
					int response = ownedItems.getInt("RESPONSE_CODE");
					if (response == 0) {
						ArrayList data = ownedItems.getStringArrayList("INAPP_PURCHASE_DATA_LIST");
						for (int i = 0; i < data.size(); ++i) {
							sendMessage(Cons.EVENT_ONPURCHASE, buildResult(new JSONObject((String) data.get(i))));
						}
						
						if (data.size() == 0) {
							sendMessage(Cons.EVENT_ONPURCHASE, buildError("empty", 66));
						}
					} else {
						sendMessage(Cons.EVENT_ONPURCHASE, buildError("Invalid response: "+response, response));
					}
					Log.d(TAG, "restore thread finished");
				} catch (final Exception e) {
					e.printStackTrace();
					sendMessage(Cons.EVENT_ONPURCHASE, buildError(e.getMessage(), null));
				}
			}
		};
		thread.setDaemon(true);
		thread.start();

		Log.d(TAG, "restore finished");
	}

	public static boolean isAvailable() {
		try {
			return get().service.isBillingSupported(3, UnityPlayer.currentActivity.getPackageName(), Cons.INAPP) == Cons.RESULT_OK;
		} catch (Exception e) {
			e.printStackTrace();
			return false;
		}
	}

	private static String buildError(String msg, Integer code) {
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
		sendMessage(Cons.EVENT_ONREADY, "");
	}

	private static void sendMessage(String message, String value) {
		UnityPlayer.UnitySendMessage(get().listener, message, value);
	}

	@Override
	public void onServiceDisconnected(ComponentName name) {
		this.service = null;
	}
	
	public void onActivityResult(int requestCode, int resultCode, Intent data) {
		Log.d(TAG, "onActivityResult start "+requestCode);
		
		try {
			if (requestCode == Cons.REQUEST_CODE_PURCHASE) {
				int responseCode = data.getIntExtra("RESPONSE_CODE", 0);
				String purchaseData = data.getStringExtra("INAPP_PURCHASE_DATA");
				if (responseCode == Activity.RESULT_OK) {
					Log.d(TAG, "onActivityResult purchase ok");
					sendMessage(Cons.EVENT_ONPURCHASE, buildResult(new JSONObject(purchaseData)));
				} else if (resultCode == Activity.RESULT_CANCELED) {
					Log.d(TAG, "onActivityResult purchase canceled");
					sendMessage(Cons.EVENT_ONPURCHASE, buildError("canceled", Cons.IAP_RESPONSE_CANCELED));
				}
			} else {
				debug("onActivityResult invalid code");
			}
		} catch (Exception e) {
			e.printStackTrace();
			debug(e.getMessage());
		}
	}
}
