package onda.lan;

import java.io.*;
import java.net.*;
import java.security.SecureRandom;
import java.util.*;
import java.util.concurrent.*;

public final class Room implements Closeable {
    private final File dir;
    private final byte[] page;
    private final ServerSocket server;
    private final Map<String,Item> items=new LinkedHashMap<String,Item>();
    private final Set<Socket> connections=new HashSet<Socket>();
    private final ExecutorService workers=Executors.newFixedThreadPool(8);
    private long reserved;
    private boolean closed;
    final String token=UUID.randomUUID().toString().replace("-","");
    static final long MAX=256L*1024*1024, TOTAL=512L*1024*1024;
    static final class Item {
        final String id,name; final File file;
        Item(String name,File dir){id=UUID.randomUUID().toString();this.name=name;file=new File(dir,id);}
    }
    public Room(File parent,InputStream html) throws IOException {
        ByteArrayOutputStream data=new ByteArrayOutputStream();
        try{copy(html,data,-1);}finally{html.close();}page=data.toByteArray();
        dir=new File(parent,"onda-"+token);
        if(!dir.mkdirs())throw new IOException("No se pudo crear la carpeta temporal");
        server=new ServerSocket(0);
        Thread listener=new Thread(new Runnable(){public void run(){listen();}},"Onda-LAN");
        listener.setDaemon(true);listener.start();
    }
    public String local(){return url("127.0.0.1");}
    private String url(String ip){return "http://"+ip+":"+server.getLocalPort()+"/"+token+"/";}
    public String address() throws SocketException {
        Enumeration<NetworkInterface> nets=NetworkInterface.getNetworkInterfaces();
        while(nets.hasMoreElements()){NetworkInterface n=nets.nextElement();
            if(!n.isUp()||n.isLoopback())continue;
            for(Enumeration<InetAddress> ips=n.getInetAddresses();ips.hasMoreElements();){
                InetAddress ip=ips.nextElement();
                if(ip instanceof Inet4Address && ip.isSiteLocalAddress())return url(ip.getHostAddress());
            }
        }return "";
    }
    private void listen(){
        while(!server.isClosed())try{
            final Socket s=server.accept();
            synchronized(this){if(closed||connections.size()>=8){s.close();continue;}connections.add(s);}
            workers.execute(new Runnable(){public void run(){try{handle(s);}catch(Exception ignored){}finally{
                try{s.close();}catch(IOException ignored){} synchronized(Room.this){connections.remove(s);}
            }}});
        }catch(Exception e){if(server.isClosed())break;}
    }
    static void copy(InputStream in,OutputStream out,long left)throws IOException{
        byte[] buf=new byte[32768];while(left!=0){int n=in.read(buf,0,left<0?buf.length:(int)Math.min(left,buf.length));
            if(n<0){if(left>0)throw new EOFException();return;}out.write(buf,0,n);if(left>0)left-=n;}
    }
    static String line(InputStream in)throws IOException{
        ByteArrayOutputStream b=new ByteArrayOutputStream();while(b.size()<8192){int c=in.read();if(c<0)throw new EOFException();
            if(c==10)return b.toString("ISO-8859-1").trim();b.write(c);}throw new IOException("Cabecera larga");
    }
    static void header(OutputStream out,String status,String mime,long len,String extra)throws IOException{
        out.write(("HTTP/1.1 "+status+"\r\nContent-Type: "+mime+"\r\nContent-Length: "+len+"\r\nConnection: close\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\nReferrer-Policy: no-referrer\r\n"+extra+"\r\n").getBytes("US-ASCII"));
    }
    static void msg(OutputStream out,String status,String body)throws IOException{byte[] bytes=body.getBytes("UTF-8");header(out,status,"text/plain; charset=utf-8",bytes.length,"");out.write(bytes);}
    private void handle(Socket s)throws Exception{
        s.setSoTimeout(30000);InputStream in=new BufferedInputStream(s.getInputStream());OutputStream out=s.getOutputStream();
        String[] req=line(in).split(" ");if(req.length!=3)return;
        Map<String,String> headers=new HashMap<String,String>();for(int i=0;;i++){if(i>=64)return;String h=line(in);if(h.isEmpty())break;
            int k=h.indexOf(':');if(k<1)return;headers.put(h.substring(0,k).toLowerCase(Locale.US),h.substring(k+1).trim());}
        String prefix="/"+token+"/";if(!req[1].startsWith(prefix)){msg(out,"404 Not Found","Sala desconocida");return;}
        String route=req[1].substring(prefix.length());boolean get=req[0].equals("GET");
        if(get && route.isEmpty()){header(out,"200 OK","text/html; charset=utf-8",page.length,"");out.write(page);return;}
        if(get && route.equals("list")){StringBuilder b=new StringBuilder();synchronized(this){for(Item a:items.values())b.append(a.id).append(' ').append(a.file.length()).append(' ').append(URLEncoder.encode(a.name,"UTF-8")).append('\n');}msg(out,"200 OK",b.toString());return;}
        if(get && route.startsWith("get/")){Item a;synchronized(this){a=items.get(route.substring(4));}
            if(a!=null){try(FileInputStream f=new FileInputStream(a.file)){header(out,"200 OK","application/octet-stream",a.file.length(),"Content-Disposition: attachment; filename*=UTF-8''"+URLEncoder.encode(a.name,"UTF-8").replace("+","%20")+"\r\n");copy(f,out,-1);}return;}}
        if(req[0].equals("POST") && route.equals("send")){
            if(!"1".equals(headers.get("x-onda"))){msg(out,"403 Forbidden","Solicitud inválida");return;}
            long size;try{size=Long.parseLong(headers.get("content-length"));}catch(Exception e){msg(out,"411 Length Required","Falta tamaño");return;}
            String name=URLDecoder.decode(headers.containsKey("x-name")?headers.get("x-name"):"archivo","UTF-8").replaceAll("[\\p{Cntrl}/\\\\]","_");
            if(name.length()>180)name=name.substring(0,180);if(name.isEmpty())name="archivo";
            Item a=new Item(name,dir);
            synchronized(this){if(size<0||size>MAX||reserved+size>TOTAL||items.size()>=512){msg(out,"413 Payload Too Large","Límite de la sala alcanzado");return;}reserved+=size;}
            boolean ok=false;try{try(FileOutputStream f=new FileOutputStream(a.file)){copy(in,f,size);}
                synchronized(this){if(closed)throw new IOException();items.put(a.id,a);ok=true;}
            }finally{if(!ok){a.file.delete();synchronized(this){reserved-=size;}}}
            msg(out,"201 Created","Listo");return;
        }msg(out,"404 Not Found","No encontrado");
    }
    public synchronized void close(){closed=true;try{server.close();}catch(IOException ignored){}
        for(Socket s:connections)try{s.close();}catch(IOException ignored){} workers.shutdownNow();
        File[] files=dir.listFiles();if(files!=null)for(File f:files)f.delete();dir.delete();
    }
}
