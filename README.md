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

