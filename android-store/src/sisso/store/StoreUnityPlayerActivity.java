package sisso.store;

import android.content.Intent;

import com.unity3d.player.UnityPlayerActivity;

public class StoreUnityPlayerActivity extends UnityPlayerActivity {
	@Override
	protected void onActivityResult(int requestCode, int resultCode, Intent data) {
		StoreService.get().onActivityResult(requestCode, resultCode, data);
	}
}
