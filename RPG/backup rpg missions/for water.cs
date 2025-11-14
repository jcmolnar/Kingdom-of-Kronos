};
		instant SimGroup "WATER water" {
			instant Marker "6200 6200 79 0" {
				dataBlock = "PathMarker";
				name = "";
				position = "-2048 0 0";
				rotation = "0 0 0";
			};
			instant SimGroup "EXITSOUND xwater.wav";
			instant SimGroup "DropPoints" {
				instant Marker "Marker1" {
					dataBlock = "PathMarker";
					name = "";
					position = "-2449 -183.125 65.0721";
					rotation = "0 0 0";
				};
				instant Marker "Marker1" {
					dataBlock = "PathMarker";
					name = "";
					position = "-2458.5 -186.75 64.8125";
					rotation = "0 0 0";
				};
				instant Marker "Marker1" {
					dataBlock = "PathMarker";
					name = "";
					position = "-2461.25 -179.625 65.071";
					rotation = "0 0 0";
				};
				instant Marker "Marker1" {
					dataBlock = "PathMarker";
					name = "";
					position = "-2469.25 -185.125 65.0111";
					rotation = "0 0 0";
				};
			};
			instant SimGroup "ENTERSOUND water3.wav";
			instant SimGroup "AMBIENTSOUND uwater.wav 40";
			instant SimGroup "MUSIC uwater2.wav 1";
		};