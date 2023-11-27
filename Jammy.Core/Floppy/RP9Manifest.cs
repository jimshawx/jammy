using System.Collections.Generic;
using System.Xml.Serialization;

namespace Jammy.Core.Floppy
{
	[XmlRoot("rp9", Namespace = "http://www.retroplatform.com")]
	public class RP9Manifest
	{
		public class Features
		{
			[XmlAttribute]
			public string requiredversion { get; set; }
			[XmlAttribute]
			public string authoringversion { get; set; }
		}

		public class Requirements
		{
			public string host { get; set; }
			public string playerversion { get; set; }
			public Features features;
		}

		public class Entity
		{
			[XmlAttribute]
			public string oid { get; set; }
			[XmlAttribute]
			public string priority { get; set; }
			[XmlAttribute]
			public string type { get; set; }

			[XmlText]
			public string entity { get; set; }
		}

		public class Distribution
		{
			[XmlAttribute]
			public string channel { get; set; }

			[XmlText]
			public string distribution { get; set; }
		}

		public class Description
		{
			public string demo { get; set; }
			public Entity entity { get; set; } = new Entity();
			public string title { get; set; }
			public string year { get; set; }
			public string rating { get; set; }
			public string systemrom { get; set; }
			public string genre { get; set; }
			public Distribution distribution { get; set; } = new Distribution();
			public string language { get; set; }
		}

		public class Clip
		{
			[XmlAttribute]
			public string left { get; set; }
			[XmlAttribute]
			public string top { get; set; }
			[XmlAttribute]
			public string width { get; set; }
			[XmlAttribute]
			public string height { get; set; }
			[XmlAttribute]
			public string version { get; set; }
		}

		public class Boot
		{
			[XmlAttribute ]
			public string type { get;set;}
			[XmlAttribute]
			public string @readonly { get;set; }
			[XmlText]
			public string boot { get;set;}
		}

		public class Ram
		{
			[XmlAttribute]
			public string type { get; set; }
			[XmlText]
			public string ram { get; set; }
		}

		public class Configuration
		{
			public string system { get; set; }
			[XmlElement("peripheral")]
			public List<string> peripheral { get; set; } = new List<string>();

			[XmlElement("compatibility")]
			public List<string> compatibility = new List<string>();

			public Clip clip { get;set;} = new Clip();
			public Boot boot { get;set;} = new Boot();
			public Ram ram { get;set;} = new Ram();
		}

		public class Floppy
		{
			[XmlAttribute]
			public string priority { get; set; }
			[XmlText]
			public string floppy { get; set; }
		}

		public class Harddrive
		{
			[XmlAttribute]
			public string priority { get; set; }
			[XmlText]
			public string harddrive { get; set; }
		}

		public class Media
		{
			[XmlElement("floppy")]
			public List<Floppy> floppy { get; set; } = new List<Floppy>();

			[XmlElement("harddrive")]
			public List<Harddrive> harddrive { get;set;} = new List<Harddrive>();
		}

		public class Image
		{
			[XmlAttribute]
			public string root { get; set; }
			[XmlAttribute]
			public string type { get; set; }
			[XmlAttribute]
			public string width { get; set; }
			[XmlAttribute]
			public string height { get; set; }
			[XmlAttribute]
			public string priority { get; set; }
			[XmlText]
			public string image { get; set; }
		}

		public class Document
		{
			[XmlAttribute]
			public string root { get; set; }
			[XmlAttribute]
			public string type { get; set; }
			[XmlAttribute]
			public string priority { get; set; }
			[XmlText]
			public string document { get; set; }
		}

		public class Extras
		{
			public Image image { get; set; } = new Image();
			public Document document { get; set; } = new Document();
		}

		public class Application
		{
			[XmlAttribute]
			public string oid { get; set; }
			[XmlAttribute]
			public string score { get; set; }
			[XmlAttribute]
			public string libraryversion { get; set; }

			public Description description { get; set; } = new Description();
			public Configuration configuration { get; set; } = new Configuration();
			public Media media { get; set; } = new Media();
			public Extras extras { get; set; } = new Extras();
		}

		[XmlElement("requirements")]
		public Requirements requirements { get; set; } = new Requirements();

		[XmlElement("application")]
		public Application application { get; set; } = new Application();
	}
}

/*
<?xml version="1.0" encoding="UTF-8"?>
<rp9 xmlns="http://www.retroplatform.com" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:schemaLocation="http://www.retroplatform.com http://www.retroplatform.com/xsd/rp9.xsd">
	<requirements>
		<host>Cloanto(R) RetroPlatform Player(TM)</host>
		<playerversion>2.2.0.0</playerversion>
		<features requiredversion="2.2.0.0" authoringversion="7.1.26.0"/>
	</requirements>
	<application oid="1.3.6.1.4.1.23153.1000.10.75.1" score="100" libraryversion="3.0">
		<description>
			<type>demo</type>
			<entity oid="1.3.6.1.4.1.23153.1000.1.83" priority="1" type="publisher">Anarchy</entity>
			<title>3D Demo</title>
			<year>1991</year>
			<rating>3</rating>
			<systemrom>120</systemrom>
			<genre>demo</genre>
			<distribution channel="amigaforever">express</distribution>
			<language>en</language>
		</description>
		<configuration>
			<system>a-500</system>
			<peripheral>a-501</peripheral>
			<compatibility>flexible-blitter-immediate</compatibility>
			<peripheral>silent-drives</peripheral>
			<clip left="260" top="52" width="1280" height="568" version="2"/>
			<compatibility>flexible-maxhorizontal-nosuperhires</compatibility>
			<compatibility>flexible-sprite-collisions-spritesplayfield</compatibility>
			<compatibility>flexible-sound</compatibility>
		</configuration>
		<media>
			<floppy priority="1">3ddemo1.adf</floppy>
			<floppy priority="2">3ddemo2.adf</floppy>
		</media>
		<extras>
			<image root="embedded" type="screen-running" width="160" height="120" priority="1">rp9-preview.png</image>
			<document root="embedded" type="help" priority="1">rp9-help-en.txt</document>
		</extras>
	</application>
</rp9>
*/
