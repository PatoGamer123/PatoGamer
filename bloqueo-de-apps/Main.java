package prestamo.mando;
import android.app.*;
import android.os.*;
import android.widget.*;
import java.net.*;
import javax.crypto.Mac;
import javax.crypto.spec.SecretKeySpec;
public class Main extends Activity {
 EditText code;TextView info;
 public void onCreate(Bundle b){super.onCreate(b);LinearLayout p=new LinearLayout(this);p.setOrientation(1);p.setPadding(30,40,30,30);setContentView(p);
 TextView title=new TextView(this);title.setText("Bloqueo de apps · 1.0");title.setTextSize(28);p.addView(title);
 code=new EditText(this);code.setHint("Dirección:puerto y clave del panel");code.setSingleLine(true);code.setSaveEnabled(false);p.addView(code);
 button(p,"Fijar ventana seleccionada","LOCK");button(p,"Soltar ventana","RETURN");info=new TextView(this);p.addView(info);}
 void button(LinearLayout p,String label,String cmd){Button b=new Button(this);b.setText(label);p.addView(b);b.setOnClickListener(v->{String input=code.getText().toString().trim();new Thread(()->send(input,cmd)).start();});}
 void show(String m){runOnUiThread(()->info.setText(m));}
 void send(String input,String cmd){try(DatagramSocket sock=new DatagramSocket()){
 String[] a=input.split("\\s+");if(a.length!=2||!a[1].matches("[0-9a-f]{48}"))throw new Exception();
 String[] dest=a[0].split(":");sock.connect(InetAddress.getByName(dest[0]),Integer.parseInt(dest[1]));sock.setSoTimeout(4000);
 byte[] hello="HELLO".getBytes("US-ASCII");sock.send(new DatagramPacket(hello,hello.length));
 byte[] data=new byte[160];DatagramPacket pkt=new DatagramPacket(data,data.length);sock.receive(pkt);
 String nonce=new String(data,0,pkt.getLength(),"US-ASCII");if(!nonce.matches("[0-9a-f]{32}"))throw new Exception();
 byte[] key=new byte[24];for(int i=0;i<24;i++)key[i]=(byte)Integer.parseInt(a[1].substring(i*2,i*2+2),16);
 Mac h=Mac.getInstance("HmacSHA256");h.init(new SecretKeySpec(key,"HmacSHA256"));StringBuilder out=new StringBuilder(cmd+" ");
 for(byte c:h.doFinal((nonce+" "+cmd).getBytes("US-ASCII")))out.append(String.format(java.util.Locale.US,"%02x",c&255));
 byte[] req=out.toString().getBytes("US-ASCII");sock.send(new DatagramPacket(req,req.length));pkt.setLength(data.length);sock.receive(pkt);
 show(new String(data,0,pkt.getLength(),"US-ASCII").equals("OK")?"Orden aceptada por Windows.":"Windows rechazó la orden. Revisa la cuenta activa y los permisos.");
 }catch(Exception e){show("Comprueba la clave, el Wi-Fi y el permiso de firewall del PC.");}}
}

