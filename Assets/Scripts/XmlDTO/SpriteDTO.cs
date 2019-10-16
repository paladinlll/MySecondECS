using System;
using UnityEngine;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.IO;

namespace XmlDTO
{
	public class Module
	{ 		
		[XmlAttribute("id")]
		public string id;
		[XmlAttribute("x")]
		public int x;
		[XmlAttribute("y")]
		public int y;
		[XmlAttribute("w")]
		public int w;
		[XmlAttribute("h")]
		public int h;
	}

	public class FModule
	{ 		
		[XmlAttribute("mid")]
		public string mid;
		[XmlAttribute("ScaleX")]
		public int ScaleX;
		[XmlAttribute("ScaleY")]
		public int ScaleY;
		[XmlAttribute("ox")]
		public int ox;
		[XmlAttribute("oy")]
		public int oy;
		[XmlAttribute("alpha")]
		public int alpha;
		[XmlAttribute("flags")]
		public int flags;
	}

	public class Frame
	{ 		
		[XmlAttribute("id")]
		public string id;
		[XmlAttribute("desc")]
		public string desc;
		[XmlElement("FMODULE")]
		public List<FModule> FModules = new List<FModule>();
	}

	public class AFrame
	{ 		
		[XmlAttribute("fid")]
		public string fid;
		[XmlAttribute("time")]
		public int time;
		[XmlAttribute("ox")]
		public int ox;
		[XmlAttribute("oy")]
		public int oy;
		[XmlAttribute("flags")]
		public int flags;
	}

	public class Animation
	{ 		
		[XmlAttribute("id")]
		public string id;
		[XmlAttribute("desc")]
		public string desc;
		[XmlElement("AFRAME")]
		public List<AFrame> AFrames = new List<AFrame>();
	}

	[XmlRoot("SPRITE")]
	public class SpriteDTO
	{
		[XmlArray("MODULES")]
		[XmlArrayItem("MODULE")]
		public List<Module> Modules = new List<Module>();

		[XmlArray("FRAMES")]
		[XmlArrayItem("FRAME")]
		public List<Frame> Frames = new List<Frame>();

		[XmlArray("ANIMATIONS")]
		[XmlArrayItem("ANIMATION")]
		public List<Animation> Animations = new List<Animation>();

        public static SpriteDTO Load(string xmlStr) {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(SpriteDTO));
            Stream s = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlStr));
            var spriteContainer = serializer.Deserialize(s) as SpriteDTO;
            s.Close();

            return spriteContainer;
        }
    }

}

