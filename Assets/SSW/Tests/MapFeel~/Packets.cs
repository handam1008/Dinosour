int checks=0;
foreach(uint id in new uint[]{0,1,uint.MaxValue}){
var spec=new SSW.BoltSpec{Style=3,Speed=15,Damage=15,Life=5,Radius=0.12f,Scale=0.65f,Stick=true,CanPenetrate=true};
var frame=new SSW.MotionFrame{Tick=40,Jump=2,Pulse=id,Move=UnityEngine.Vector2.right,Aim=UnityEngine.Vector2.up};
var state=new SSW.MotionState{Epoch=4,Jump=2,Pulse=id,Position=new UnityEngine.Vector2(2,3),Velocity=new UnityEngine.Vector2(4,5),Scale=1};
using(var writer=new Unity.Netcode.FastBufferWriter(512,Unity.Collections.Allocator.Temp)){
writer.WriteNetworkSerializable(spec);writer.WriteNetworkSerializable(frame);writer.WriteNetworkSerializable(state);
using(var reader=new Unity.Netcode.FastBufferReader(writer,Unity.Collections.Allocator.Temp)){
reader.ReadNetworkSerializable(out SSW.BoltSpec bolt);reader.ReadNetworkSerializable(out SSW.MotionFrame input);reader.ReadNetworkSerializable(out SSW.MotionState pose);
if(!spec.Equals(bolt)||!bolt.CanPenetrate||input.Pulse!=id||input.Tick!=40||!input.Valid||pose.Pulse!=id||pose.Epoch!=4||pose.Position!=state.Position||pose.Velocity!=state.Velocity)throw new System.Exception("Merged protocol round trip failed");
checks++;
}}}
System.IO.File.WriteAllText("Logs/MapFeel25/Packets.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{checks}));return new{checks};