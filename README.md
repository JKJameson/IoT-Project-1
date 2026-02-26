# IoT Project 1
....
 

# Rain Sensor Notes
....
We originally assumed the rain sensor would show a value of 0-255 depending on how much moisture was detected.
Upon testing, we discovered that the sensor has the following values.

| Moisture | Sensor Reading |
|---------|----------|
| Dry| 316 |
| A few drops of water| 60 |
| Completely saturated | 33 |

So the active "raining" figure is probably somewhere between 75 to 33 but this will need to be fine tuned for real-world tests when it is raining.
