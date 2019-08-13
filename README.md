# bq24195-mkr-wifi-debug

Arduino sketch plus WPF desktop app for trying to get +5V boost (via USB OTG functionality of BQ24195) on the Arudino MKR WiFi 1010.

To get working:

- Edit sketch and substitute in your wifi ID / password
- Install sketch and open serial monitor
- Jot down the IP address.. on my WiFi, it doesn't seem to change with restarts

- Build & run the desktop app
- Make sure the IP address/hostname is correct
- Click Connect
- Play with the controls

Note, this was a quick tool and is very flaky. Sometimes it needs a restart since the queue for requests to the device gets stalled for some reason.

