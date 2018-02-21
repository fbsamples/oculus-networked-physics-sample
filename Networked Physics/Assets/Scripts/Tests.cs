/**
 * Copyright (c) 2017-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the Scripts directory of this source tree. An additional grant 
 * of patent rights can be found in the PATENTS file in the same directory.
 */

using System;
using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;

public static class Tests
{
    static void test_bitpacker()
    {
        Debug.Log( "test_bitpacker" );

        const int BufferSize = 256;

        uint[] buffer = new uint[BufferSize];

        var writer = new Network.BitWriter();

        writer.Start( buffer );

        Assert.IsTrue( writer.GetTotalBytes() == BufferSize );
        Assert.IsTrue( writer.GetBitsWritten() == 0 );
        Assert.IsTrue( writer.GetBytesWritten() == 0 );
        Assert.IsTrue( writer.GetBitsAvailable() == BufferSize * 8 );

        writer.WriteBits( 0, 1 );
        writer.WriteBits( 1, 1 );
        writer.WriteBits( 10, 8 );
        writer.WriteBits( 255, 8 );
        writer.WriteBits( 1000, 10 );
        writer.WriteBits( 50000, 16 );
        writer.WriteBits( 9999999, 32 );

        writer.Finish();

        const int bitsWritten = 1 + 1 + 8 + 8 + 10 + 16 + 32;

        Assert.IsTrue( writer.GetBytesWritten() == 10 );
        Assert.IsTrue( writer.GetTotalBytes() == BufferSize );
        Assert.IsTrue( writer.GetBitsWritten() == bitsWritten );
        Assert.IsTrue( writer.GetBitsAvailable() == BufferSize * 8 - bitsWritten );

        int bytesWritten = writer.GetBytesWritten();

        const int ExpectedBytesWritten = 10;

        Assert.IsTrue( bytesWritten == ExpectedBytesWritten );

        byte[] readBuffer = writer.GetData();

        Assert.IsTrue( readBuffer.Length == ExpectedBytesWritten );

        var reader = new Network.BitReader();

        reader.Start( readBuffer );

        Assert.IsTrue( reader.GetBitsRead() == 0 );
        Assert.IsTrue( reader.GetBitsRemaining() == bytesWritten * 8 );

        uint a = reader.ReadBits( 1 );
        uint b = reader.ReadBits( 1 );
        uint c = reader.ReadBits( 8 );
        uint d = reader.ReadBits( 8 );
        uint e = reader.ReadBits( 10 );
        uint f = reader.ReadBits( 16 );
        uint g = reader.ReadBits( 32 );

        reader.Finish();

        Assert.IsTrue( a == 0 );
        Assert.IsTrue( b == 1 );
        Assert.IsTrue( c == 10 );
        Assert.IsTrue( d == 255 );
        Assert.IsTrue( e == 1000 );
        Assert.IsTrue( f == 50000 );
        Assert.IsTrue( g == 9999999 );

        Assert.IsTrue( reader.GetBitsRead() == bitsWritten );
        Assert.IsTrue( reader.GetBitsRemaining() == bytesWritten * 8 - bitsWritten );
    }

    struct TestStruct
    {
        public bool bool_value;
        public int int_value;
        public uint uint_value;
        public uint bits_value;
    };

    class TestSerializer : Network.Serializer
    {
        public void WriteTestStruct( Network.WriteStream stream, ref TestStruct testStruct )
        {
            write_bool( stream, testStruct.bool_value );
            write_int( stream, testStruct.int_value, -100, +100 );
            write_uint( stream, testStruct.uint_value, 100, 1000 );
            write_bits( stream, testStruct.bits_value, 23 );
        }

        public void ReadTestStruct( Network.ReadStream stream, out TestStruct testStruct )
        {
            read_bool( stream, out testStruct.bool_value );
            read_int( stream, out testStruct.int_value, -100, +100 );
            read_uint( stream, out testStruct.uint_value, 100, 1000 );
            read_bits( stream, out testStruct.bits_value, 23 );
        }
    }

    static void test_serialization()
    {
        Debug.Log( "test_serialization" );

        const int MaxPacketSize = 1024;

        var serializer = new TestSerializer();

        var buffer = new uint[MaxPacketSize/4];

        var writeStream = new Network.WriteStream();

        writeStream.Start( buffer );

        TestStruct input;
        input.bool_value = true;
        input.int_value = -5;
        input.uint_value = 215;
        input.bits_value = 12345;

        serializer.WriteTestStruct( writeStream, ref input );

        writeStream.Finish();

        byte[] packet = writeStream.GetData();

        var readStream = new Network.ReadStream();

        readStream.Start( packet );

        TestStruct output;
        serializer.ReadTestStruct( readStream, out output );

        readStream.Finish();

        Assert.AreEqual( input.bool_value, output.bool_value );
        Assert.AreEqual( input.int_value, output.int_value );
        Assert.AreEqual( input.uint_value, output.uint_value );
        Assert.AreEqual( input.bits_value, output.bits_value );
    }

    struct TestPacketData
    {
        public ushort sequence;
    };                  

    static void test_sequence_buffer()
    {
        Debug.Log( "test_sequence_buffer" );

        const int Size = 256;

        var sequenceBuffer = new Network.SequenceBuffer<TestPacketData>( Size );

        for ( int i = 0; i < Size; ++i )
        {
            TestPacketData entry;
            entry.sequence = 0;
            Assert.IsTrue( sequenceBuffer.Exists( (ushort)i ) == false );
            Assert.IsTrue( sequenceBuffer.Available( (ushort)i ) == true );
            Assert.IsTrue( sequenceBuffer.Find( (ushort)i ) == -1 );
        }

        for ( int i = 0; i <= Size*4; ++i )
        {
            int index = sequenceBuffer.Insert( (ushort) i );
            Assert.IsTrue( index != -1 );
            Assert.IsTrue( sequenceBuffer.GetSequence() == i + 1 );
            sequenceBuffer.Entries[index].sequence = (ushort) i;
        }

        for ( int i = 0; i <= Size; ++i )
        {
            // note: outside bounds!
            int index = sequenceBuffer.Insert( (ushort) i );
            Assert.IsTrue( index == -1 );
        }    

        ushort sequence = Size * 4;
        for ( int i = 0; i < Size; ++i )
        {
            int index = sequenceBuffer.Find( sequence );
            Assert.IsTrue( index >= 0 );
            Assert.IsTrue( index < Size );
            Assert.IsTrue( sequenceBuffer.Entries[index].sequence == sequence );
            sequence--;
        }

        sequenceBuffer.Reset();

        Assert.IsTrue( sequenceBuffer.GetSequence() == 0 );

        for ( int i = 0; i < Size; ++i )
        {
            Assert.IsTrue( sequenceBuffer.Exists( (ushort)i ) == false );
            Assert.IsTrue( sequenceBuffer.Available( (ushort)i ) == true );
            Assert.IsTrue( sequenceBuffer.Find( (ushort)i ) == -1 );
        }
    }

    struct TestPacketData32
    {
        public uint sequence;
    };                  

    static void test_sequence_buffer32()
    {
        Debug.Log( "test_sequence_buffer32" );

        const int Size = 256;

        var sequenceBuffer = new Network.SequenceBuffer32<TestPacketData32>( Size );

        for ( int i = 0; i < Size; ++i )
        {
            TestPacketData entry;
            entry.sequence = 0;
            Assert.IsTrue( sequenceBuffer.Exists( (uint) i ) == false );
            Assert.IsTrue( sequenceBuffer.Available( (uint) i ) == true );
            Assert.IsTrue( sequenceBuffer.Find( (uint) i ) == -1 );
        }

        for ( int i = 0; i <= Size * 4; ++i )
        {
            int index = sequenceBuffer.Insert( (uint) i );
            Assert.IsTrue( index != -1 );
            Assert.IsTrue( sequenceBuffer.GetSequence() == i + 1 );
            sequenceBuffer.Entries[index].sequence = (uint) i;
        }

        for ( int i = 0; i <= Size; ++i )
        {
            // note: outside bounds!
            int index = sequenceBuffer.Insert( (uint) i );
            Assert.IsTrue( index == -1 );
        }

        uint sequence = Size * 4;
        for ( int i = 0; i < Size; ++i )
        {
            int index = sequenceBuffer.Find( sequence );
            Assert.IsTrue( index >= 0 );
            Assert.IsTrue( index < Size );
            Assert.IsTrue( sequenceBuffer.Entries[index].sequence == sequence );
            sequence--;
        }

        sequenceBuffer.Reset();

        Assert.IsTrue( sequenceBuffer.GetSequence() == 0 );

        for ( int i = 0; i < Size; ++i )
        {
            Assert.IsTrue( sequenceBuffer.Exists( (uint) i ) == false );
            Assert.IsTrue( sequenceBuffer.Available( (uint) i ) == true );
            Assert.IsTrue( sequenceBuffer.Find( (uint) i ) == -1 );
        }
    }

    static void test_generate_ack_bits()
    {
        Debug.Log( "test_generate_ack_bits" );

        const int Size = 256;

        var receivedPackets = new Network.SequenceBuffer<TestPacketData>( Size );

        ushort ack = 0xFFFF;
        uint ack_bits = 0xFFFFFFFF;

        Network.Util.GenerateAckBits( receivedPackets, out ack, out ack_bits );
        Assert.IsTrue( ack == 0xFFFF );
        Assert.IsTrue( ack_bits == 0 );

        for ( int i = 0; i <= Size; ++i )
            receivedPackets.Insert( (ushort) i );

        Network.Util.GenerateAckBits( receivedPackets, out ack, out ack_bits );
        Assert.IsTrue( ack == Size );
        Assert.IsTrue( ack_bits == 0xFFFFFFFF );

        receivedPackets.Reset();

        ushort[] input_acks = { 1, 5, 9, 11 };
        for ( int i = 0; i < input_acks.Length; ++i )
            receivedPackets.Insert( input_acks[i] );

        Network.Util.GenerateAckBits( receivedPackets, out ack, out ack_bits );

        Assert.IsTrue( ack == 11 );
        Assert.IsTrue( ack_bits == ( 1 | (1<<(11-9)) | (1<<(11-5)) | (1<<(11-1)) ) );
    }

    static void test_connection()
    {
        Debug.Log( "test_connection" );

        var sender = new Network.Connection();
        var receiver = new Network.Connection();

        const int NumIterations = 256;

        for ( int i = 0; i < NumIterations; ++i )
        {
            Network.PacketHeader senderPacketHeader;
            Network.PacketHeader receiverPacketHeader;

            sender.GeneratePacketHeader( out senderPacketHeader );
            receiver.GeneratePacketHeader( out receiverPacketHeader );

            if ( ( i % 11 ) != 0 )
                sender.ProcessPacketHeader( ref receiverPacketHeader );

            if ( ( i % 13 ) != 0 )
                receiver.ProcessPacketHeader( ref senderPacketHeader );
        }

        ushort[] senderAcks = new ushort[Network.Connection.MaximumAcks]; 
        ushort[] receiverAcks = new ushort[Network.Connection.MaximumAcks]; 

        int numSenderAcks = 0;
        int numReceiverAcks = 0;

        sender.GetAcks( ref senderAcks, ref numSenderAcks );
        receiver.GetAcks( ref receiverAcks, ref numReceiverAcks );

        Assert.IsTrue( numSenderAcks > NumIterations / 2 );
        Assert.IsTrue( numReceiverAcks > NumIterations / 2 );

        var senderAcked = new bool[NumIterations];
        var receiverAcked = new bool[NumIterations];
                                    
        for ( int i = 0; i < NumIterations/2; ++i )
        {
            senderAcked[senderAcks[i]] = true;
            receiverAcked[receiverAcks[i]] = true;
        }

        for ( int i = 0; i < NumIterations/2; ++i )
        {
            Assert.IsTrue( senderAcked[i] == ( ( i % 13 ) != 0 ) );
            Assert.IsTrue( receiverAcked[i] == ( ( i % 11 ) != 0 ) );
        }
    }

    static void test_delta_buffer()
    {
#if !DEBUG_AUTHORITY

        Debug.Log( "test_delta_buffer" );

        const int NumCubeStates = 5;

        const int DeltaBufferSize = 256;

        var deltaBuffer = new DeltaBuffer( DeltaBufferSize );

        CubeState cubeState = CubeState.defaults;

        // check that querying for a sequence number not in the buffer returns false

        const ushort Sequence = 100;
        const ushort ResetSequence = 1000;

        bool result = deltaBuffer.GetCubeState( Sequence, ResetSequence, 0, ref cubeState );

        Assert.IsTrue( result == false );

        // now add an entry for the sequence number

        result = deltaBuffer.AddPacket( Sequence, ResetSequence );

        Assert.IsTrue( result );

        // add a few cube states for the packet

        int[] cubeIds = new int[NumCubeStates];
        CubeState[] cubeStates = new CubeState[NumCubeStates];

        for ( int i = 0; i < NumCubeStates; ++i )
        {
            cubeStates[i] = CubeState.defaults;
            cubeStates[i].position_x = i;

            int cubeId = 10 + i * 10;

            cubeIds[i] = cubeId;

            result = deltaBuffer.AddCubeState( Sequence, cubeId, ref cubeStates[i] );

            Assert.IsTrue( result );
        }

        // verify that we can find the cube state we added by cube id and sequence

        for ( int i = 0; i < NumCubeStates; ++i )
        {
            int cubeId = 10 + i * 10;

            result = deltaBuffer.GetCubeState( Sequence, ResetSequence, cubeId, ref cubeState );

            Assert.IsTrue( result );
            Assert.IsTrue( cubeState.position_x == cubeStates[i].position_x );
        }

        // verify that get cube state returns false for cube ids that weren't in this packet

        for ( int i = 0; i < Constants.NumCubes; ++i )
        {
            bool validCubeId = false;

            for ( int j = 0; j < NumCubeStates; ++j )
            {
                if ( cubeIds[j] == i )
                {
                    validCubeId = true;
                }
            }

            if ( validCubeId )
                continue;

            result = deltaBuffer.GetCubeState( Sequence, ResetSequence, i, ref cubeState );

            Assert.IsTrue( result == false );
        }

        // grab the packet data for the sequence and make sure it matches what we expect

        int packetNumCubes;
        int[] packetCubeIds;
        CubeState[] packetCubeState;
        
        result = deltaBuffer.GetPacketData( Sequence, ResetSequence, out packetNumCubes, out packetCubeIds, out packetCubeState );

        Assert.IsTrue( result == true );
        Assert.IsTrue( packetNumCubes == NumCubeStates );

        for ( int i = 0; i < NumCubeStates; ++i )
        {
            Assert.IsTrue( packetCubeIds[i] == 10 + i * 10 );
            Assert.IsTrue( packetCubeState[i].position_x == cubeStates[i].position_x );
        }
        
        // try to grab packet data for an invalid sequence number and make sure it returns false

        result = deltaBuffer.GetPacketData( Sequence + 1, ResetSequence, out packetNumCubes, out packetCubeIds, out packetCubeState );

        Assert.IsTrue( result == false );

        // try to grab packet data for a different reset sequence number and make sure it returns false

        result = deltaBuffer.GetPacketData( Sequence, ResetSequence + 1, out packetNumCubes, out packetCubeIds, out packetCubeState );

        Assert.IsTrue( result == false );

#endif // #if !DEBUG_AUTHORITY
    }

    static void test_signed_unsigned()
    {
        Debug.Log( "test_signed_unsigned" );

        int[] expectedValues = { 0, -1, +1, -2, +2, -3, +3, -4, +4, -5, +5, -6, +6 };

        for ( int i = 0; i < expectedValues.Length; ++i )
        {
            int signed = Network.Util.UnsignedToSigned( (uint) i );

            Assert.IsTrue( signed == expectedValues[i] );

            uint unsigned = Network.Util.SignedToUnsigned( signed );

            Assert.IsTrue( unsigned == (uint) i );
        }
    }

    public static void RunTests()
    {
        Assert.raiseExceptions = true;

        test_bitpacker();
        test_serialization();
        test_sequence_buffer();
        test_sequence_buffer32();
        test_generate_ack_bits();
        test_connection();
        test_delta_buffer();
        test_signed_unsigned();

        Debug.Log( "All tests completed successfully!" );
    }
}
