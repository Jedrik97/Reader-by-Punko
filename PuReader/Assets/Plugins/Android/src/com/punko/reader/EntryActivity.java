package com.punko.reader;

import com.unity3d.player.UnityPlayerActivity;
import com.unity3d.player.UnityPlayer;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;

public class EntryActivity extends UnityPlayerActivity {
    @Override
    protected void onCreate(Bundle b) {
        super.onCreate(b);
        handleIntent(getIntent());
    }

    @Override
    protected void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        setIntent(intent);
        handleIntent(intent);
    }

    private void handleIntent(Intent intent) {
        if (intent == null) return;
        Uri uri = intent.getData();
        if (uri != null) {
            UnityPlayer.UnitySendMessage("App", "OnExternalOpen", uri.toString());
        }
    }
}
