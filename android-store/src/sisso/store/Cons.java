package sisso.store;

public class Cons {
	public static final int REQUEST_CODE_PURCHASE = 666;
	public static final String INAPP = "inapp";
	// http://developer.android.com/google/play/billing/billing_reference.html
	public static final int RESULT_OK = 0;
	public static final int RESULT_CANCELED = 1;
	protected static final int RESULT_OWNED = 7;
	public static final String IAP_BIND = "com.android.vending.billing.InAppBillingService.BIND";
	public static final String EVENT_ONPURCHASE = "OnPurchase";
	public static final String EVENT_ONCONSUME = "OnConsume";
	public static final String EVENT_ONINFO = "OnInfo";
	public static final String EVENT_ONREADY = "OnReady";
	protected static final String MESSAGE_CODE_FAILED= "failed";
	protected static final String MESSAGE_CODE_EMPTY = "empty";
	public static final String MESSAGE_CODE_CANCELED = "canceled";
}
