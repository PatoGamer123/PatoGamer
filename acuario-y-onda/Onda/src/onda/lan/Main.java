package onda.lan;
import android.app.*;
import android.content.*;
import android.os.*;
import android.widget.*;
import android.view.*;
import android.net.Uri;
public class Main extends Activity {
    private TextView status;
    private Button open,share;
    private android.os.Handler handler=new android.os.Handler();
    private final Runnable update=new Runnable(){public void run(){
        Room r=Host.room;open.setEnabled(r!=null);share.setEnabled(r!=null);
        try{String link=r==null?"":r.address();status.setText(r==null?Host.message:link.isEmpty()?"Conecta el Wi-Fi para compartir la sala.":link);}catch(Exception e){status.setText("Comprueba el Wi-Fi");}
        handler.postDelayed(this,1200);
    }};
    Button button(LinearLayout p,String label,View.OnClickListener action){Button b=new Button(this);b.setText(label);p.addView(b);b.setOnClickListener(action);return b;}
    public void onCreate(Bundle state){super.onCreate(state);
        ScrollView scroll=new ScrollView(this);LinearLayout p=new LinearLayout(this);p.setPadding(34,35,34,35);p.setOrientation(1);scroll.addView(p);setContentView(scroll);
        TextView title=new TextView(this);title.setText("Onda LAN");title.setTextSize(34);p.addView(title);
        status=new TextView(this);status.setTextIsSelectable(true);p.addView(status);
        button(p,"Crear sala",v->{Intent i=new Intent(this,Host.class);if(Build.VERSION.SDK_INT>=26)startForegroundService(i);else startService(i);});
        open=button(p,"Enviar y recibir",v->{if(Host.room!=null)browse(Host.room.local());});
        share=button(p,"Compartir enlace",v->{try{String link=Host.room.address();if(link.isEmpty())return;Intent i=new Intent(Intent.ACTION_SEND);i.setType("text/plain");i.putExtra(Intent.EXTRA_TEXT,link);startActivity(Intent.createChooser(i,"Enlace de la sala"));}catch(Exception e){toast("No se pudo compartir");}});
        button(p,"Cerrar sala",v->stopService(new Intent(this,Host.class)));
        EditText address=new EditText(this);address.setSingleLine();address.setInputType(17);address.setHint("Enlace de otra sala");p.addView(address);
        button(p,"Unirse",v->browse(address.getText().toString().trim()));
    }
    private void toast(String s){Toast.makeText(this,s,Toast.LENGTH_LONG).show();}
    private void browse(String s){try{Uri u=Uri.parse(s);if(!"http".equals(u.getScheme())||u.getHost()==null){toast("Pega el enlace completo de la sala");return;}startActivity(new Intent(Intent.ACTION_VIEW,u));}catch(Exception e){toast("Se necesita un navegador compatible");}}
    protected void onResume(){super.onResume();update.run();}
    protected void onPause(){handler.removeCallbacks(update);super.onPause();}
}
