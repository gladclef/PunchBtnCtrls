/***************************************************
 * Note: modified by Ben Bean to be used for the punchbutton project.
 * 
  This is a library for the Adafruit 1.8" SPI display.

This library works with the Adafruit 1.8" TFT Breakout w/SD card
  ----> http://www.adafruit.com/products/358
The 1.8" TFT shield
  ----> https://www.adafruit.com/product/802
The 1.44" TFT breakout
  ----> https://www.adafruit.com/product/2088
as well as Adafruit raw 1.8" TFT display
  ----> http://www.adafruit.com/products/618

  Check out the links above for our tutorials and wiring diagrams
  These displays use SPI to communicate, 4 or 5 pins are required to
  interface (RST is optional)
  Adafruit invests time and resources providing this open source code,
  please support Adafruit and open-source hardware by purchasing
  products from Adafruit!

  Written by Limor Fried/Ladyada for Adafruit Industries.
  MIT license, all text above must be included in any redistribution
 ****************************************************/

#include <Adafruit_GFX.h>    // Core graphics library
#include <Adafruit_ST7735.h> // Hardware-specific library for ST7735
#include <Adafruit_ST7789.h> // Hardware-specific library for ST7789
#include <SPI.h>


// For the breakout, you can use any 2 or 3 pins
// These pins will also work for the 1.8" TFT shield
#define TFT_CS     10
#define TFT_RST    9  // you can also connect this to the Arduino reset
                       // in which case, set this #define pin to -1!
#define TFT_DC     8

#define WIDTH 160
#define HEIGHT 128


#define SPACE 32
#define COLON 58
#define ZERO 48
#define NINE 57

//#define BAUD_9600
//#define BAUD_57600
//#define BAUD_115200
#define BAUD_250000

#ifdef BAUD_9600
#define BAUD 9600
#define CHAR_COUNT_DELAY 30
#define CHAR_READ_DELAY 10
#define SERIAL_WRITE_DELAY 30
#endif
#ifdef BAUD_57600
#define BAUD 57600
#define CHAR_COUNT_DELAY 5
#define CHAR_READ_DELAY 2
#define SERIAL_WRITE_DELAY 5
#endif
#ifdef BAUD_115200
#define BAUD 115200
#define CHAR_COUNT_DELAY 2
#define CHAR_READ_DELAY_MICROS 600
#define SERIAL_WRITE_DELAY 2
#endif
#ifdef BAUD_250000
#define BAUD 250000
#define CHAR_COUNT_DELAY 1
#define CHAR_READ_DELAY_MICROS 300
#define SERIAL_WRITE_DELAY 1
#endif

#define Neutral 0
#define Press 1
#define Up 2
#define Down 3
#define Right 4
#define Left 5

// Option 1 (recommended): must use the hardware SPI pins
// (for UNO thats sclk = 13 and sid = 11) and pin 10 must be
// an output. This is much faster - also required if you want
// to use the microSD card (see the image drawing example)

// For 1.44" and 1.8" TFT with ST7735 use
Adafruit_ST7735 tft = Adafruit_ST7735(TFT_CS,  TFT_DC, TFT_RST);

// For 1.54" TFT with ST7789
//Adafruit_ST7789 tft = Adafruit_ST7789(TFT_CS,  TFT_DC, TFT_RST);

// Option 2: use any pins but a little slower!
//#define TFT_SCLK 13   // set these to be whatever pins you like!
//#define TFT_MOSI 11   // set these to be whatever pins you like!
//Adafruit_ST7735 tft = Adafruit_ST7735(TFT_CS, TFT_DC, TFT_MOSI, TFT_SCLK, TFT_RST);

uint16_t rowBuf[WIDTH];
unsigned char allSerial[65];
unsigned char *incomingBytes;
boolean serialDebugging = false;
uint16_t currReadX = 0;
uint16_t currReadY = 0;
uint16_t colSpan = 1;
uint16_t rowSpan = 1;
uint8_t activityIndicator = 1;
uint16_t colorPalette[256];
uint8_t colorPaletteIdx = 0;

void setup(void) {
  Serial.begin(BAUD);

  // Use this initializer if you're using a 1.8" TFT
  tft.initR(INITR_BLACKTAB);   // initialize a ST7735S chip, black tab

  // Use this initializer (uncomment) if you're using a 1.44" TFT
  //tft.initR(INITR_144GREENTAB);   // initialize a ST7735S chip, black tab

  // Use this initializer (uncomment) if you're using a 0.96" 180x60 TFT
  //tft.initR(INITR_MINI160x80);   // initialize a ST7735S chip, mini display

  // Use this initializer (uncomment) if you're using a 1.54" 240x240 TFT
  //tft.init(240, 240);   // initialize a ST7789 chip, 240x240 pixels

	tft.setRotation(1);
  tft.fillScreen(ST77XX_BLACK);
}

uint8_t currentFillShift = 0x00;
uint16_t currentFillColor = 0x00;

void loop() {
  // read the next serial message
  uint16_t numBytes = readSerial();
  
  if (numBytes != 0)
  {
    if (interpretSerial(numBytes))
    {
		  Serial.println("ACK");
			delay(SERIAL_WRITE_DELAY);
    }
	}

	int8_t joy = checkJoystick();
	//printJoystick(joy);
	if (joy != Neutral)
	{
		switch (joy)
		{
			case Press:
			case Up:
				currentFillColor++;
				break;
			case Right:
				currentFillShift++;
				break;
			case Left:
				currentFillShift--;
				break;
			case Down:
				currentFillColor--;
				break;
		}
//		char currentFillStr[40];
//		sprintf(currentFillStr, "%02x:%02x %04x", currentFillShift, currentFillColor, currentFillColor << currentFillShift);
//		Serial.println(currentFillStr);
//		tft.fillScreen(currentFillColor << currentFillShift);
//		delay(150);
	}
}

void drawRow(uint16_t rowIdx) {
	bool drawActivityIndicator = true;
	
	if (colSpan == 1 && rowSpan == 1) {
		for (uint16_t i = 0; i < WIDTH; i++)
		{
			tft.drawPixel(i, rowIdx, rowBuf[i]);
		}
		drawActivityIndicator = (rowIdx < HEIGHT-1);
	} else {
		uint16_t lineSize = WIDTH / colSpan;
		uint16_t y = rowIdx * rowSpan;
//		uint8_t ye = y + rowSpan;
		for (uint8_t i = 0; i < lineSize; i++)
		{
			tft.fillRect(i * colSpan, y, colSpan, rowSpan, rowBuf[i]);
		}
	}

	if (drawActivityIndicator)
	{
		tft.drawPixel(activityIndicator-1, HEIGHT-1, 0x0000);
		tft.drawPixel(activityIndicator, HEIGHT-1, 0xFFFF);
		activityIndicator = (activityIndicator >= WIDTH) ? 1 : activityIndicator+1;
	}
}

// Check the joystick position
// from https://learn.adafruit.com/1-8-tft-display/reading-the-joystick
int8_t checkJoystick()
{
  int8_t joystickState = analogRead(3);
  
  if (joystickState < 50) return Left;
  if (joystickState < 150) return Down;
  if (joystickState < 250) return Press;
  if (joystickState < 500) return Right;
  if (joystickState < 650) return Up;
  return Neutral;
}

// from https://learn.adafruit.com/1-8-tft-display/reading-the-joystick
void printJoystick(int8_t joy) 
{
  switch (joy)
  {
    case Left:
      Serial.println("Left");
      break;
    case Right:
      Serial.println("Right");
      break;
    case Up:
      Serial.println("Up");
      break;
    case Down:
      Serial.println("Down");
      break;
    case Press:
      Serial.println("Press");
      break;
  }
}

/*-*********************************************************
 * THE FOLLOWING IS FROM MY ARDUINO SERIAL CONTROL LIBRARY *
 **********************************************************/

 /**
 * Interprets the next command from the serial console, as read by readSerial(...).
 * 
 * @return true if the command was valid, false for a bad command
 */
boolean interpretSerial(uint16_t numBytes)
{
  uint16_t lineRead = numBytes-1;
  unsigned char command = incomingBytes[0];
  unsigned char *data = incomingBytes+1;
	bool isDrawLineCommand = false;
  
  switch (command)
  {
    case 'C': // Color command: set the testing fill color
    	uint8_t idx;
    	idx = 0;
    	if (numBytes < 2)
    		return false;
    	currentFillColor = (uint8_t)data[idx++] - (uint8_t)'0';
    	if (numBytes >= idx+1)
    		currentFillColor = (currentFillColor * 10) + ((uint8_t)data[idx++] - (uint8_t)'0');
    	if (numBytes >= idx+1)
    		currentFillColor = (currentFillColor * 10) + ((uint8_t)data[idx++] - (uint8_t)'0');
    	if (numBytes >= idx+1)
    		currentFillColor = (currentFillColor * 10) + ((uint8_t)data[idx++] - (uint8_t)'0');
    	if (numBytes >= idx+1)
    		currentFillColor = (currentFillColor * 10) + ((uint8_t)data[idx++] - (uint8_t)'0');
    	Serial.print("read color: ");
    	Serial.println(currentFillColor);
			tft.fillScreen(currentFillColor << currentFillShift);
    	break;
    case 'L': // Line Part command: draw the next line part of the image
//    	Serial.println("Draw");
//  		delay(SERIAL_WRITE_DELAY);
      for (uint16_t i = 0; i < lineRead; i+= 2)
      {
  			rowBuf[currReadX]  = ((uint8_t)data[i + 0]) << 8;
  			rowBuf[currReadX] |=  (uint8_t)data[i + 1];
      	currReadX++;
      }
      isDrawLineCommand = true;
      break;
    case 'l': // Line Part command but using the color pallete
      for (uint16_t i = 0; i < lineRead; i++)
      {
  			rowBuf[currReadX++] = colorPalette[data[i]];
      }
      isDrawLineCommand = true;
      break;
    case 'P': // Palette command, for setting the color palette used for drawing
//    	Serial.println("Palette");
//  		delay(SERIAL_WRITE_DELAY);
      for (uint16_t i = 0; i < lineRead; i+= 2)
      {
  			colorPalette[colorPaletteIdx]  = ((uint8_t)data[i + 0]) << 8;
  			colorPalette[colorPaletteIdx] |=  (uint8_t)data[i + 1];
    		colorPaletteIdx++;
      }
    	return true;
    case 'R': // restart image or pallete drawing
//    	Serial.println("Restart");
//  		delay(SERIAL_WRITE_DELAY);
			switch (data[0])
			{
				case 'L':
		    	currReadX = 0;
		    	currReadY = 0;
		    	break;
	    	case 'P':
	    		colorPaletteIdx = 0;
	    		break;
    		default:
    			return false;
			}
			return true;
    case 'S': // Span Size command
      if (numBytes < 5)
      	return false;
    	colSpan  = ((uint8_t)data[0]) << 8;
    	colSpan |= ((uint8_t)data[1]);
    	rowSpan  = ((uint8_t)data[2]) << 8;
    	rowSpan |= ((uint8_t)data[3]);
    	return true;
    default:
			char errStr[40];
			sprintf(errStr, "Unrecognized command \"%c\" (%d)", (char)command, (uint8_t)command);
    	Serial.println(errStr);
  		delay(SERIAL_WRITE_DELAY);
      return false;
  } // switch (command)

  if (isDrawLineCommand)
  {
    if (currReadX * colSpan >= WIDTH)
    {
    	drawRow(currReadY);
    	currReadX = 0;
    	currReadY++;
    }
    return true;
  }
  
  return false;
}

/**
 * Reads the byte value from the given chars array, from the start position to 
 * (start + length - 1) position.
 */
unsigned char readByte(unsigned char *chars, byte start, byte length)
{
  byte retval = 0;
  byte end = start + length;
  for (byte i = start; i < end; i++)
  {
    retval = retval * 10 + (chars[i] - ZERO);
  }
  return retval;
}

/**
 * Reads the next message from the serial console.
 * 
 * Expected format is:
 * a:b
 * Where "a" is the number of bytes to be read, and "b" is those bytes.
 * 
 * @return "a" the number of bytes actually read
 */
uint16_t readSerial()
{
  uint16_t numBytes, allSerialCnt, next;
  byte nextChar;
  
  // check for serial data
  if (!Serial.available())
  {
    return NULL;
  }

  // how many bytes to read?
  numBytes = 0;
  allSerialCnt = 0;
  for (nextChar = 0; Serial.available(); delay(CHAR_COUNT_DELAY))
  {
    nextChar = Serial.read();
    allSerial[allSerialCnt++] = nextChar;
    if (nextChar == COLON)
    {
      break;
    }
    if (nextChar < ZERO || nextChar > NINE)
    {
      eatSerial();
      char charString[40];
      sprintf(charString, "error reading character \"%c\" for bytes count\0", nextChar);
      Serial.println(charString);
  		delay(SERIAL_WRITE_DELAY);
      return 0;
    }
    numBytes = numBytes * 10 + (nextChar - ZERO);
  } // for (byte nextChar = 0; Serial.available(); delay(30))
  incomingBytes = allSerial + allSerialCnt;

  // let the serial input finish reading
#ifdef CHAR_READ_DELAY
  delay(numBytes * CHAR_READ_DELAY);
#elif CHAR_READ_DELAY_MICROS
	delayMicroseconds(numBytes * CHAR_READ_DELAY_MICROS);
#endif
  
  // read up to the next numBytes bytes
  for (next = 0; Serial.available() && next < numBytes; next++)
  {
    allSerial[allSerialCnt++] = Serial.read();
  } // for (byte next = 0; Serial.available(); next++)
  
  return (next);
}

/**
 * Clears the serial buffer.
 */
void eatSerial()
{
  while (Serial.available())
  {
    Serial.read();
  }
}

