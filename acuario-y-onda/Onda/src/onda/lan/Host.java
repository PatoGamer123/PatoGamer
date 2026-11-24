package onda.lan;
import android.app.*;
import android.content.*;
import android.os.*;
public class Host extends Service {
    static volatile Room room;
    static volatile String message="Crea una sala o pega el enlace de otro dispositivo.";
    public int onStartCommand(Intent intent,int flags,int id){
        Notification.Builder builder;
        if(Build.VERSION.SDK_INT>=26){
            NotificationManager manager=(NotificationManager)getSystemService(NOTIFICATION_SERVICE);
            manager.createNotificationChannel(new NotificationChannel("sala","Sala local",NotificationManager.IMPORTANCE_LOW));
            builder=new Notification.Builder(this,"sala");
        }else builder=new Notification.Builder(this);
        PendingIntent go=PendingIntent.getActivity(this,0,new Intent(this,Main.class),PendingIntent.FLAG_IMMUTABLE);
        startForeground(1,builder.setContentTitle("Onda LAN").setContentText("Sala local activa").setSmallIcon(android.R.drawable.stat_sys_upload).setContentIntent(go).setOngoing(true).build());
        if(room==null)try{room=new Room(getCacheDir(),getAssets().open("room.html"));}catch(Exception e){message="No se pudo iniciar la sala.";stopSelf();}
        return START_NOT_STICKY;
    }
    public void onDestroy(){Room r=room;room=null;if(r!=null)r.close();super.onDestroy();}
    public IBinder onBind(Intent i){return null;}
}
