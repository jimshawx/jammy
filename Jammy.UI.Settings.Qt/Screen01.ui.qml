

/*
This is a UI file (.ui.qml) that is intended to be edited in Qt Design Studio only.
It is supposed to be strictly declarative and only uses a subset of QML. If you edit
this file manually, you might introduce QML code that is not supported by Qt Design Studio.
Check out https://doc.qt.io/qtcreator/creator-quick-ui-forms.html for details on .ui.qml files.
*/
import QtQuick
import QtQuick.Controls

ApplicationWindow {
    width: 800
    height: 600

    visible: true

}
/*
	Rectangle {
		
		id: settings
		width: 800
		height: 600

		color: "#7f7f7f"
		
		ComboBox {
			id: cbQuickStart
			x: 23
			y: 21
			width: 331
			height: 40
		}

		Button {
			id: btnQuickStart
			x: 360
			y: 21
			text: qsTr("Quick Start")
		}

		GroupBox {
			id: groupBox4
			x: 310
			y: 103
			width: 525
			height: 200
			padding: 0
			topPadding: 0
			title: qsTr("Floppy Disk")

			SpinBox {
				id: nudFloppyCount
				x: 385
				y: 25
				value: 1
				to: 4
			}

			TextField {
				id: txtDF0
				x: 0
				y: 25
				width: 344
				height: 40
			}

			TextField {
				id: txtDF1
				x: 0
				y: 66
				width: 344
				height: 40
			}

			TextField {
				id: txtDF2
				x: 0
				y: 106
				width: 344
				height: 40
			}

			TextField {
				id: txtDF3
				x: 0
				y: 148
				width: 344
				height: 40
			}

			Button {
				id: btnDF0Pick
				x: 350
				y: 25
				width: 29
				height: 40
				text: qsTr("...")
			}

			Button {
				id: btnDF1Pick
				x: 350
				y: 66
				width: 29
				height: 40
				text: qsTr("...")
			}

			Button {
				id: btnDF2Pick
				x: 350
				y: 106
				width: 29
				height: 40
				text: qsTr("...")
			}

			Button {
				id: btnDF3Pick
				x: 350
				y: 148
				width: 29
				height: 40
				text: qsTr("...")
			}
		}

		GroupBox {
			id: groupBox5
			x: 310
			y: 309
			width: 525
			height: 97
			padding: 0
			topPadding: 0
			title: qsTr("Hard Disk")

			ComboBox {
				id: cbDiskController
				x: 0
				y: 29
				width: 275
				height: 40
				model: ListModel {
					ListElement {
						sku: "None"
					}
					ListElement {
						sku: "A600_A1200"
					}
					ListElement {
						sku: "A3000"
					}
					ListElement {
						sku: "A4000"
					}
				}
			}

			SpinBox {
				id: nudHardDiskCount
				x: 385
				y: 23
			}
		}

		GroupBox {
			id: groupBox6
			x: 310
			y: 412
			width: 525
			height: 83
			padding: 0
			topPadding: 0
			title: qsTr("Kickstart")

			TextField {
				id: txtKickstart
				x: 0
				y: 34
				width: 344
				height: 40
				placeholderText: qsTr("Text Field")
			}

			Button {
				id: btnROMPick
				x: 350
				y: 34
				width: 29
				text: qsTr("...")
			}
		}

		GroupBox {
			id: groupBox7
			x: 310
			y: 505
			width: 228
			height: 70
			padding: 0
			topPadding: 0
			title: qsTr("Miscellaneous")

			CheckBox {
				id: cbAudio
				x: 0
				y: 27
				text: qsTr("Audio")
				rotation: 0
			}

			CheckBox {
				id: cbDebugging
				x: 93
				y: 27
				text: qsTr("Debugging")
			}
		}

		GroupBox {
			id: groupBox8
			x: 544
			y: 505
			width: 291
			height: 70
			topPadding: 0
			padding: 0
			title: qsTr("Blitter")

			RadioButton {
				id: rbImmediate
				x: 12
				y: 26
				text: qsTr("Immediate")
			}

			RadioButton {
				id: rbSynchronous
				x: 136
				y: 26
				text: qsTr("Synchronous")
				checked: true
			}
		}

		GroupBox {
			id: groupBox2
			x: 23
			y: 232
			width: 281
			height: 118
			padding: 0
			topPadding: 0
			title: qsTr("Chipset")

			ComboBox {
				id: cbChipset
				x: 12
				y: 34
				model: ListModel {
					ListElement {
						sku: "OCS"
					}
					ListElement {
						sku: "ECS"
					}
					ListElement {
						sku: "AGA"
					}
				}
			}

			RadioButton {
				id: rbPAL
				x: 168
				y: 34
				text: qsTr("PAL")
			}

			RadioButton {
				id: rbNTSC
				x: 168
				y: 70
				text: qsTr("NTSC")
			}
		}

		GroupBox {
			id: groupBox1
			x: 23
			y: 79
			width: 281
			height: 147
			padding: 0
			leftPadding: 0
			topPadding: 0
			title: qsTr("CPU")

			ComboBox {
				id: cbSku
				x: 12
				y: 44
				model: ListModel {
					ListElement {
						sku: "MC68000"
					}
					ListElement {
						sku: "MC68EC020"
					}
					ListElement {
						sku: "MC68030"
					}
					ListElement {
						sku: "MC68040"
					}
				}
			}

			RadioButton {
				id: rbNative
				x: 158
				y: 34
				text: qsTr("Native")
			}

			RadioButton {
				id: rbMusashi
				x: 158
				y: 67
				text: qsTr("Musashi")
			}

			RadioButton {
				id: rbMusashiCS
				x: 158
				y: 102
				text: qsTr("Musashi C#")
			}
		}

		GroupBox {
			id: groupBox3
			x: 25
			y: 371
			width: 279
			height: 311
			padding: 0
			topPadding: 0
			title: qsTr("Memory")

			Text {
				id: _text
				x: 12
				y: 34
				width: 24
				height: 16
				color: "#0078d7"
				text: qsTr("Chip")
				font.pixelSize: 12
			}

			Text {
				id: _text2
				x: 12
				y: 80
				color: "#0078d7"
				text: qsTr("Trapdoor")
				font.pixelSize: 12
			}

			Text {
				id: _text3
				x: 12
				y: 126
				color: "#0078d7"
				text: qsTr("Zorro II")
				font.pixelSize: 12
			}

			Text {
				id: _text4
				x: 12
				y: 172
				color: "#0078d7"
				text: qsTr("Zorro III")
				font.pixelSize: 12
			}

			Text {
				id: _text5
				x: 12
				y: 218
				color: "#0078d7"
				text: qsTr("Motherboard")
				font.pixelSize: 12
			}

			Text {
				id: _text6
				x: 12
				y: 264
				color: "#0078d7"
				text: qsTr("CPU Slot")
				font.pixelSize: 12
			}

			ComboBox {
				id: dudChipRAM
				x: 98
				y: 22
				model: ListModel {
					ListElement {
						sku: "0.5"
					}
					ListElement {
						sku: "1.0"
					}
					ListElement {
						sku: "2.0"
					}
				}
			}

			ComboBox {
				id: dudTrapdoor
				x: 98
				y: 68
				model: ListModel {
					ListElement {
						sku: "0"
					}
					ListElement {
						sku: "0.5"
					}
					ListElement {
						sku: "1.0"
					}

					ListElement {
						sku: "1.5"
					}
					ListElement {
						sku: "1.75"
					}
				}
			}

			ComboBox {
				id: dudZ2
				x: 98
				y: 114
				model: ListModel {
					ListElement {
						sku: "0"
					}

					ListElement {
						sku: "0.5"
					}

					ListElement {
						sku: "1.0"
					}

					ListElement {
						sku: "2.0"
					}

					ListElement {
						sku: "4.0"
					}
					ListElement {
						sku: "8.0"
					}
				}
			}
			ComboBox {
				id: dudZ3
				x: 98
				y: 160
				model: ListModel {
					ListElement {
						sku: "0"
					}

					ListElement {
						sku: "128"
					}

					ListElement {
						sku: "256"
					}

					ListElement {
						sku: "512"
					}

					ListElement {
						sku: "1024"
					}
					ListElement {
						sku: "256+256"
					}
					ListElement {
						sku: "512+512"
					}
					ListElement {
						sku: "512+512+512"
					}
				}
			}
			ComboBox {
				id: dudMotherboard
				x: 98
				y: 206
				model: ListModel {
					ListElement {
						sku: "0"
					}

					ListElement {
						sku: "8"
					}

					ListElement {
						sku: "16"
					}

					ListElement {
						sku: "32"
					}

					ListElement {
						sku: "64"
					}
				}
			}
			ComboBox {
				id: dudCPUSlot
				x: 98
				y: 252
				model: ListModel {
					ListElement {
						sku: "0"
					}

					ListElement {
						sku: "8"
					}

					ListElement {
						sku: "16"
					}

					ListElement {
						sku: "32"
					}

					ListElement {
						sku: "64"
					}
					ListElement {
						sku: "128"
					}
				}
			}
		}

		Button {
			id: btnExit
			x: 318
			y: 634
			text: qsTr("Exit")
		}

		Button {
			id: btnLoad
			x: 428
			y: 634
			text: qsTr("Load...")
		}

		Button {
			id: btnSave
			x: 534
			y: 634
			text: qsTr("Save")
		}

		Button {
			id: btnSaveAs
			x: 640
			y: 634
			text: qsTr("Save As...")
		}

		Button {
			id: btnGo
			x: 746
			y: 634
			text: qsTr("Go!")
			font.styleName: "Italic"
		}
	}
}
*/
