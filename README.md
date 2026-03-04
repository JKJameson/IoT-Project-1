# IoT Project 1
This is a weather station with a focus on detection of rain cycles. The aim of the project is to notify the user when it has stopped raining, to do productive things outside.
 

# Rain Sensor Notes
We originally assumed the rain sensor would show a value of 0-255 depending on how much moisture was detected.
Upon testing, we discovered that the sensor has the following values.

| Moisture | Sensor Reading Day 1 | Sensor Reading Day 2 |
|---------|----------|----------|
| Dry| 316 | 1000 |
| A few drops of water| 60 | 34 |
| Completely saturated | 33 | 34 |

So the active "raining" figure is probably somewhere between 75 to 33 but this will need to be fine tuned for real-world tests when it is raining.

Day 2 testing gave us a higher base reading for when the sensor was dry (1000 reading).


![Image](WhatsApp%20Image%202026-03-02%20at%202.31.45%20PM%20(1).jpeg?raw=true)
![Image](WhatsApp%20Image%202026-03-02%20at%202.31.45%20PM%20(2).jpeg?raw=true)
![Image](WhatsApp%20Image%202026-03-02%20at%202.31.45%20PM.jpeg?raw=true)
![Image](WhatsApp%20Image%202026-03-02%20at%202.31.46%20PM%20(1).jpeg?raw=true)
![Image](WhatsApp%20Image%202026-03-02%20at%202.31.46%20PM%20(2).jpeg?raw=true)
![Image](WhatsApp%20Image%202026-03-02%20at%202.31.46%20PM%20(3).jpeg?raw=true)
![Image](WhatsApp%20Image%202026-03-02%20at%202.31.46%20PM.jpeg?raw=true)

# IoT Project 1
![Video](WhatsApp%20Video%202026-03-02%20at%202.31.46%20PM%20(1).mp4)
![Video](WhatsApp%20Video%202026-03-02%20at%202.31.46%20PM.mp4)

Testing carried out on Tuesday 3rd March 2026
Finding on project
Loop was slower to output readings without OLED connected to arduino

Security Concerns
Over the course of the project we deduced that during the transmission of data from the rain sensor to the user's phone there could be the risk of fabrication of data. An individual with plans of stealing rain sensor data and sending in false reports could break into the system.
A possible solution that we came up with was data encryption (HTTP to HTTPS) to name an example. By doing this we could prevent man in the middle attacks as well as interception.
We could also create anomaly detecting code to detect abnormal changes in the readings for example sudden spikes in temperature reading. This could be done by adding a range validator that would reject fictitious readings (temperatures of over 200 degrees).
We could also include timestamps when sending over data to prevent attackers from sending over once validated readings.
A lesser security threat could be simple eavesdropping whereby attackers do not tamper with data but monitor and record it. This could allow attackers to pinpoint end users' locations and sell that information. This can be prevented as simply as using a secure Wi-Fi connection such as a WPA3 connection with a strong password. This problem can also be solved through end to end encryption in the Arduino IDE code. Such an example being;
Encrypted_Data = AES(data, device_secret_key)
Send(Encrypted_Data)
There could also arise the problem of route hijacking whereby data is diverted to other unknown ends. This could be handled by using VPN tunnels instead of relying on open routing. This would ensure that data is protected even though routing changes.
