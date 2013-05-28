package sisso.store;

import java.io.PrintStream;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.io.Writer;
import java.util.ArrayList;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import android.app.PendingIntent;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.ServiceConnection;
import android.os.AsyncTask;
import android.os.Bundle;
import android.os.IBinder;
import android.os.RemoteException;
import android.util.Log;

import com.android.vending.billing.IInAppBillingService;
import com.unity3d.player.UnityPlayer;

public class StoreService implements ServiceConnection {
	private static final String TAG = "StoreService";
	private static StoreService instance;

	private IInAppBillingService service;
	private String listener;

	public static StoreService get() {
		if (instance == null)
			instance = new StoreService();
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
					if (response != 0) {
						sendMessage("OnInfo", buildError("Invalid response code: "+response));
						return;
					}

					ArrayList<String> responseList = skuDetails
							.getStringArrayList("DETAILS_LIST");
					for (String thisResponse : responseList) {
						sendMessage("OnInfo", buildResult(new JSONObject(thisResponse)));
					}
					Log.d(TAG, "getInfo thread finished");
				} catch (Exception e) {
					e.printStackTrace();
					sendMessage("OnInfo", buildError(e.getMessage()));
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
					if (response == 0) {
						PendingIntent pendingIntent = buyIntentBundle.getParcelable("BUY_INTENT");
						UnityPlayer.currentActivity.startIntentSenderForResult(
								pendingIntent.getIntentSender(), 1001,
								new Intent(), Integer.valueOf(0),
								Integer.valueOf(0), Integer.valueOf(0));
					} else {
						sendMessage("OnPurchase", buildError("Invalid response code: "+response));
					}
					Log.d(TAG, "purchase thread finish");
				} catch (final Exception e) {
					e.printStackTrace();
					sendMessage("OnPurchase", buildError(e.getMessage()));
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
					if (response == 0) {
						sendMessage("Consume", buildResult(null));
					} else {
						sendMessage("Consume", buildError("Invalid response code: "+String.valueOf(response)));
					}
					Log.d(TAG, "consume thread finish");
				} catch (final Exception e) {
					e.printStackTrace();
					sendMessage("OnConsume", buildError(e.getMessage()));
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
						ArrayList data = ownedItems.getStringArrayList("INAPP_PURCHASE_DATA");
						for (int i = 0; i < data.size(); ++i) {
							sendMessage("OnPurchase", buildResult(new JSONObject((String) data.get(i))));
						}
					} else {
						sendMessage("OnPurchase", buildError("Invalid response: "+response));
					}
					Log.d(TAG, "restore thread finished");
				} catch (final Exception e) {
					sendMessage("OnPurchase", buildError(e.getMessage()));
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

	private static String buildError(String msg) {
		try {
			JSONObject map = new JSONObject();
			map.put("ok", false);
			map.put("error", msg);
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
		sendMessage("OnReady", "");
	}

	private static void sendMessage(String message, String value) {
		UnityPlayer.UnitySendMessage(get().listener, message, value);
	}

	@Override
	public void onServiceDisconnected(ComponentName name) {
		this.service = null;
	}
}
