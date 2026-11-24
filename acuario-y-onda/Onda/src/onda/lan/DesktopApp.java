package onda.lan;
import javax.swing.*;
import java.awt.*;
import java.awt.datatransfer.StringSelection;
import java.io.*;
import java.net.*;
public class DesktopApp {
    static void browse(String url){try{Desktop.getDesktop().browse(new URI(url));}catch(Exception e){JOptionPane.showMessageDialog(null,url);}}
    public static void main(String[] args)throws Exception{
        final Room room=new Room(new File(System.getProperty("java.io.tmpdir")),DesktopApp.class.getResourceAsStream("/room.html"));
        Runtime.getRuntime().addShutdownHook(new Thread(new Runnable(){public void run(){room.close();}}));
        SwingUtilities.invokeLater(new Runnable(){public void run(){try{
            String link=room.address();
            JFrame f=new JFrame("Onda LAN");f.setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
            JPanel p=new JPanel();p.setLayout(new BoxLayout(p,BoxLayout.Y_AXIS));p.setBorder(BorderFactory.createEmptyBorder(20,24,20,24));f.add(p);
            JLabel title=new JLabel("Onda LAN");title.setFont(title.getFont().deriveFont(30f));p.add(title);
            p.add(new JLabel("Sala activa · conecta los dispositivos a la misma red Wi-Fi."));
            p.add(Box.createVerticalStrut(12));JTextField addr=new JTextField(link.isEmpty()?"Conecta la red y vuelve a abrir Onda LAN":link,50);addr.setEditable(false);p.add(addr);
            JPanel buttons=new JPanel(new FlowLayout(FlowLayout.LEFT));
            JButton open=new JButton("Enviar y recibir");open.addActionListener(e->browse(room.local()));buttons.add(open);
            JButton copy=new JButton("Copiar enlace");copy.setEnabled(!link.isEmpty());copy.addActionListener(e->Toolkit.getDefaultToolkit().getSystemClipboard().setContents(new StringSelection(link),null));buttons.add(copy);p.add(buttons);
            p.add(new JLabel("Abre el enlace en el navegador del otro equipo. Al cerrar esta ventana termina la sala."));
            f.pack();f.setLocationRelativeTo(null);f.setVisible(true);
        }catch(Exception e){JOptionPane.showMessageDialog(null,e.toString());System.exit(1);}}});
    }
}
