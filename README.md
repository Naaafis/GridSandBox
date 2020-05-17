# GridSandBox
Simple Xbox Kinect game

Code for point system + randomized visible grids. Players can virtually grab color coded boxes from timed grids to earn points. 

Main code found in "MainWindow.xaml.cs"


##############################################################################################################################
GridSandbox Overview

The goal of this project is to get familiar with basics of programming and then write simple programming logic for a game like Tic-Tac-Toe based on the provided functions. The base code implements Tic-Tac-Toe with a player competing against an algorithm implemented in a function called Bot. The students will then modify this and other functions to make their own game.


The GUI

Visually, the game consists of three windows. On the left is a skeleton window to show what the
Kinect detects. The box around the active hand indicates the area where the hand can operate. In the middle is the grid window which consists of M by N cells (default is M = N = 3), and on the right is the instruction window where a set of brief game instructions is displayed. Below the grid window is a status bar where the game status can be announced (e.g., a winner).


Student Work Area:

void studentWork()


Program Overview

studentWork function is the location where students write their code to implement game logic. Almost all of their code should be placed within this function. Consult a UTF if you plan on writing
anywhere outside of this function. This is referred to as the Sandbox area.
The studentWork function (and all its code) will be executed about 30 times per second (fps).

Global variables:
These are variables that the students should change in order to change how their games
operate. If you need to add more global variables, please consult a UTF. Global variables are consistent throughout the entire program.

  ● const int numGridRows = 3, numGridCols = 3;
    These variables can be changed to set the grid to a desired size. The larger the numbers, the
    smaller the cells.
  ● bool useLeftHand = true;
    On true, the left hand is tracked by the Kinect Sensor, on false, the right hand is tracked.
  ● int[,] gridArray;
    gridArray is a dynamically-allocated (meaning it can be updated at any point in time) array, set to the size numGridRows x       
    numGridCols. This array is intended to be used when implementing game logic.

Functions:

The functions below need to be accessed in order to interact with the Kinect and the GUI (Graphical User Interface). Most of the functionality students need to complete for their project should come from these functions. Consult a UTF before adding any new functions.

  GUI Functions
    ● void clearBoard() - Clear all colored cells on the grid and set them to white.
    ● void clearGridLocation(int row, int col) - Clear the color at a given grid location
      specified by (row, col) and reset to white.
    ● void setText(string text, System.Windows.Media.Brush brush) - Set the status box below the grid to a given string with a      
      given color.


  Some sample brushes are as follows (more can be found from the autofill pull-down):
  System.Windows.Media.Brushes.Black System.Windows.Media.Brushes.White System.Windows.Media.Brushes.Green      
  System.Windows.Media.Brushes.Red

    ● void setInstructionText(string text, System.Windows.Media.Brush brush, double fontSize) – Sets the instructions to the  
      right side of the grid to a given string, color, and size.

    For example:
    setInstructionText("This is an example of a list! \r\n 1. Connect 3 in a row \r\n 2. Beat the Bot \r\n 3. Game repeats!" +    
    "\r\n 4. \r\n 5. \r\n 6. \r\n 7. \r\n 8. \r\n", System.Windows.Media.Brushes.Black, 48);
    Note that "\r\n" produce a new line in the string.
    ● void setButtonText(string text, int num ) – update the text of a button given by a
    number (1,2, or 3).

  Grid Functions
    ● void highlightGridLocationWithColor(int row, int col, System.Windows.Media.Brush brush) – update the color of a grid    
      square given by row and column.
    ● void highlightGridLocation(int row, int col, int playerNumber) – Highlights a given grid location with a player’s color   
      (accepted player numbers are 1 and 2). This does NOT edit gridArray.
    ● void zeroGridArray() – Sets the gridArray to all zeros. This function is called upon for grid initialization.
    ● To edit gridArray, the syntax is: gridArray[row, col] = playerNumber; Kinect/Player Functions
    ● void setPlayerColor(int playerNumber, System.Windows.Media.Brush brush) – Sets a player’s color to a given brush color  
      (accepted player numbers are 1 and 2).
    ● bool isPlayerHandClosed(int playerNumber) – If true, the given’s player hand is detected as closed. if false, it is 
      detected as not closed (open).
    ● Point getPlayerHandLocation(int playerNumber) – Returns a Point data structure whose coordinates are normalized to 0 to   
      1 in the hand region specified on the screen. (Note: values can be returned outside this range if the hand has left the 
      region!)
    ● bool isPlayerHandInGridLocation(int row, int col, int playerNumber) – If the player’s hand is at the specified location, 
      the function returns true, otherwise, false. This can be used to identify where player’s hand is.
      Some of the functions used by Tic-Tac-Toe (logic implemented in the example game):
    ● int checkWinner() - Checks the gridArray to determine which player wins. If no one has won yet, it returns 0.
    ● void resetGame() - Clears and resets the game state.
    ● void botMove() – Make the computer AI perform a move in tic tac toe. The bot fills the top-
      left to bottom-down.
