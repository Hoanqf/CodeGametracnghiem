   M S B A M L   `   `   `  ����   ServerTN    QuizServer.KTServer   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml       � Title$  Quiz Server��$	��540��$	��850�� � WindowStartupLocation$ CenterScreen=� � 
ResizeMode$ 	CanResize=����#2C2F48 � 	Resources    ModernButton �� 
TargetType �� Property �� Value �� 
TargetType �� CornerRadius	 �� Triggers
 P� Property P� Value  
LabelStyle��%\  &	        &	 �    �� " ��.���  � �� �#5C6BC0��  � ��
 �������  � ��$ 14(���  � ��$ Segoe UI)���  � ��$
 10,6q���  � ��$	 6,0q���  � ��$ 0q���  �@�Cursor$
 Hand����  � �� �� " ��.R��� #��z��$ 6��._��� $��Center=�$��Center=�	 P� 
 �@J�IsMouseOver . . ���  � �� �#3F51B5P� 
 � {� .  . ���  � �� �#9FA8DA��  � �� �#ECEFF1�� " ��.���  � ��$ Segoe UI)���  � ��$ 14(���  � �� �#ECEFF1��  � ��$ Center=���  � ��$	 6,0q�.��� $��25q� � RowDefinitions �� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $��* �.+���  �� Orientation$ 
Horizontal=�$��0��$��0,0,0,15q�.+��� $��
🔌 Port:��#��[ $��SemiBold"��� -    ��Name$ txtPort��$	��100��$
��5000��$
��10,0q�$��14(�$��6q����#F5F5F5���#5C6BC0$��1q��� -    ��Name$ btnStartServer��#��[  .���� $ 
Horizontal=�.+��� $	��▶��$��16(�$��0,0,5,0q��� $��Start Server���� -   $ btnStopServer��#��[  {�.  .���� $ 
Horizontal=�.+��� $	��■��$��16(�$��0,0,5,0q��� $
��Stop���� -    ��Name$ lblServerStatus��#��[ $
��Bold"����#66BB6A�� $ 
Horizontal=�$��1��$��0,0,0,15q�.+��� $��💾 Database:��#��[ $��SemiBold"��� -   $ lblDbStatus��#��[ ���#29B6F6�� -   $ lblQuestionCount��#��[ ���#AED581$
��20,0q��� $ 
Horizontal=�$��2��$��0,0,0,15q�.+��� -   $ btnStartQuiz��#��[  {�.  .���� $ 
Horizontal=�.+��� $
��🧠��$��16(�$��0,0,5,0q��� $��
Start Quiz���� -   $ lblQuizStatus��#��[ ���#FFEE58$
��Bold"��� $��📜 Server Logs��$��4��$��10q�$
��Bold"����#5C6BC0$��1q����#FDFDFD$��0,10,0,0q����#212121.���� -	   $ txtLog��$��Consolas)�$��13(� �� 
IsReadOnly .  �� TextWrapping$
 Wrap=�" �� VerticalScrollBarVisibility$
 Auto=����#FAFAFA���#CFD8DC$��1q�