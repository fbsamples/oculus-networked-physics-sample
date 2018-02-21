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

namespace Network
{
    public static class Constants
    {
        public const int MaxStringLength = 255;

        public const int STREAM_ERROR_NONE = 0;
        public const int STREAM_ERROR_OVERFLOW = 1;
        public const int STREAM_ERROR_ALIGNMENT = 2;
        public const int STREAM_ERROR_VALUE_OUT_OF_RANGE = 3;
    };

    public static class Util
    {
        public static uint SignedToUnsigned( int n )
        {
            return (uint) ( ( n << 1 ) ^ ( n >> 31 ) );
        }

        public static int UnsignedToSigned( uint n )
        {
            return (int) ( ( n >> 1 ) ^ ( -( n & 1 ) ) );
        }

        public static void GenerateAckBits<T>( SequenceBuffer<T> sequenceBuffer, out ushort ack, out uint ack_bits )
        {
            ack = (ushort) ( sequenceBuffer.GetSequence() - 1 );
            ack_bits = 0;
            uint mask = 1;
            for ( int i = 0; i < 32; ++i )
            {
                ushort sequence = (ushort) ( ack - i );
                if ( sequenceBuffer.Exists( sequence ) )
                    ack_bits |= mask;
                mask <<= 1;
            }
        }

        public static bool SequenceGreaterThan( ushort s1, ushort s2 )
        {
            return ( ( s1 > s2 ) && ( s1 - s2 <= 32768 ) ) || 
                   ( ( s1 < s2 ) && ( s2 - s1  > 32768 ) );
        }

        public static bool SequenceLessThan( ushort s1, ushort s2 )
        {
            return SequenceGreaterThan( s2, s1 );
        }

        public static int BaselineDifference( ushort current, ushort baseline )
        {
            if ( current > baseline )
            {
                return current - baseline;
            }
            else
            {
                return (ushort) ( ( ( (uint) current ) + 65536 ) - baseline );
            }
        }

        public static uint SwapBytes( uint value )
        {
            return ( ( value & 0x000000FF ) << 24 ) |
                   ( ( value & 0x0000FF00 ) << 8 ) |
                   ( ( value & 0x00FF0000 ) >> 8 ) |
                   ( ( value & 0xFF000000 ) >> 24 );
        }

        public static uint HostToNetwork( uint value )
        {
            if ( BitConverter.IsLittleEndian )
                return value;
            else
                return SwapBytes( value );
        }

        public static uint NetworkToHost( uint value )
        {
            if ( BitConverter.IsLittleEndian )
                return value;
            else
                return SwapBytes( value );
        }

        public static int PopCount( uint value )
        {
            value = value - ( ( value >> 1 ) & 0x55555555 );
            value = ( value & 0x33333333 ) + ( ( value >> 2 ) & 0x33333333 );
            value = ( ( value + ( value >> 4 ) & 0xF0F0F0F ) * 0x1010101 ) >> 24;
            return unchecked( (int) value );
        }

        public static int Log2( uint x )
        {
            uint a = x | ( x >> 1 );
            uint b = a | ( a >> 2 );
            uint c = b | ( b >> 4 );
            uint d = c | ( c >> 8 );
            uint e = d | ( d >> 16 );
            uint f = e >> 1;
            return PopCount( f );
        }

        public static int BitsRequired( int min, int max )
        {
            return ( min == max ) ? 1 : Log2( (uint) ( max - min ) ) + 1;
        }

        public static int BitsRequired( uint min, uint max )
        {
            return ( min == max ) ? 1 : Log2( max - min ) + 1;
        }
    }

    public class BitWriter
    {
        uint[] m_data;
        ulong m_scratch;
        int m_numBits;
        int m_numWords;
        int m_bitsWritten;
        int m_wordIndex;
        int m_scratchBits;
            
        public void Start( uint[] data )
        {
            Assert.IsTrue( data != null );
            m_data = data;
            m_numWords = data.Length / 4;
            m_numBits = m_numWords * 32;
            m_bitsWritten = 0;
            m_wordIndex = 0;
            m_scratch = 0;
            m_scratchBits = 0;
        }

        public void WriteBits( uint value, int bits )
        {
            Assert.IsTrue( bits > 0 );
            Assert.IsTrue( bits <= 32 );
            Assert.IsTrue( m_bitsWritten + bits <= m_numBits );

            value &= (uint) ( ( ( (ulong)1 ) << bits ) - 1 );

            m_scratch |= ( (ulong) value ) << m_scratchBits;

            m_scratchBits += bits;

            if ( m_scratchBits >= 32 )
            {
                Assert.IsTrue( m_wordIndex < m_numWords );
                m_data[m_wordIndex] = Util.HostToNetwork( (uint) ( m_scratch & 0xFFFFFFFF ) );
                m_scratch >>= 32;
                m_scratchBits -= 32;
                m_wordIndex++;
            }

            m_bitsWritten += bits;
        }

        public void WriteAlign()
        {
            int remainderBits = (int) ( m_bitsWritten % 8 );
            if ( remainderBits != 0 )
            {
                uint zero = 0;
                WriteBits( zero, 8 - remainderBits );
                Assert.IsTrue( ( m_bitsWritten % 8 ) == 0 );
            }
        }

        public void WriteBytes( byte[] data, int bytes )
        {
            Assert.IsTrue( GetAlignBits() == 0 );
            for ( int i = 0; i < bytes; ++i )
                WriteBits( data[i], 8 );
        }

        public void Finish()
        {
            if ( m_scratchBits != 0 )
            {
                Assert.IsTrue( m_wordIndex < m_numWords );
                m_data[m_wordIndex] = Util.HostToNetwork( (uint) ( m_scratch & 0xFFFFFFFF ) );
                m_scratch >>= 32;
                m_scratchBits -= 32;
                m_wordIndex++;                
            }
        }

        public int GetAlignBits()
        {
            return ( 8 - ( m_bitsWritten % 8 ) ) % 8;
        }

        public int GetBitsWritten()
        {
            return m_bitsWritten;
        }

        public int GetBitsAvailable()
        {
            return m_numBits - m_bitsWritten;
        }

        public byte[] GetData()
        {
            int bytesWritten = GetBytesWritten();
            byte[] output = new byte[bytesWritten];
            Buffer.BlockCopy( m_data, 0, output, 0, bytesWritten );
            return output;
        }

        public int GetBytesWritten()
        {
            return ( m_bitsWritten + 7 ) / 8;
        }

        public int GetTotalBytes()
        {
            return m_numWords * 4;
        }
    }

    public class BitReader
    {
        uint[] m_data;
        ulong m_scratch;
        int m_numBits;
        int m_numWords;
        int m_bitsRead;
        int m_scratchBits;
        int m_wordIndex;

        public void Start( byte[] data )
        {
            int bytes = data.Length;
            m_numWords = ( bytes + 3 ) / 4;
            m_numBits = bytes * 8;
            m_bitsRead = 0;
            m_scratch = 0;
            m_scratchBits = 0;
            m_wordIndex = 0;
            m_data = new uint[m_numWords];
            Buffer.BlockCopy( data, 0, m_data, 0, bytes );
        }

        public bool WouldOverflow( int bits )
        {
            return m_bitsRead + bits > m_numBits;
        }

        public uint ReadBits( int bits )
        {
            Assert.IsTrue( bits > 0 );
            Assert.IsTrue( bits <= 32 );
            Assert.IsTrue( m_bitsRead + bits <= m_numBits );

            m_bitsRead += bits;

            Assert.IsTrue( m_scratchBits >= 0 && m_scratchBits <= 64 );

            if ( m_scratchBits < bits )
            {
                Assert.IsTrue( m_wordIndex < m_numWords );
                m_scratch |= ( (ulong) ( Util.NetworkToHost( m_data[m_wordIndex] ) ) ) << m_scratchBits;
                m_scratchBits += 32;
                m_wordIndex++;
            }

            Assert.IsTrue( m_scratchBits >= bits );

            uint output = (uint) ( m_scratch & ( (((ulong)1)<<bits) - 1 ) );

            m_scratch >>= bits;
            m_scratchBits -= bits;

            return output;
        }

        public bool ReadAlign()
        {
            int remainderBits = m_bitsRead % 8;
            if ( remainderBits != 0 )
            {
                uint value = ReadBits( 8 - remainderBits );
                Assert.IsTrue( m_bitsRead % 8 == 0 );
                if ( value != 0 )
                    return false;
            }
            return true;
        }

        public void ReadBytes( byte[] data, int bytes )
        {
            Assert.IsTrue( GetAlignBits() == 0 );
            for ( int i = 0; i < bytes; ++i )
                data[i] = (byte) ReadBits( 8 );
        }

        public void Finish()
        {
            // ...
        }

        public int GetAlignBits()
        {
            return ( 8 - m_bitsRead % 8 ) % 8;
        }

        public int GetBitsRead()
        {
            return m_bitsRead;
        }

        public int GetBytesRead()
        {
            return m_wordIndex * 4;
        }

        public int GetBitsRemaining()
        {
            return m_numBits - m_bitsRead;
        }

        public int GetBytesRemaining()
        {
            return GetBitsRemaining() / 8;
        }
    }

    public class WriteStream
    {
        BitWriter m_writer = new BitWriter();
        int m_error = Constants.STREAM_ERROR_NONE;

        public void Start( uint[] buffer )
        {
            m_writer.Start( buffer );
        }

        public void SerializeSignedInteger( int value, int min, int max )
        {
            Assert.IsTrue( min < max );
            Assert.IsTrue( value >= min );
            Assert.IsTrue( value <= max );
            int bits = Util.BitsRequired( min, max );
            uint unsigned_value = (uint) ( value - min );
            m_writer.WriteBits( unsigned_value, bits );
        }

        public void SerializeUnsignedInteger( uint value, uint min, uint max )
        {
            Assert.IsTrue( min < max );
            Assert.IsTrue( value >= min );
            Assert.IsTrue( value <= max );
            int bits = Util.BitsRequired( min, max );
            uint unsigned_value = value - min;
            m_writer.WriteBits( unsigned_value, bits );
        }

        public void SerializeBits( byte value, int bits )
        {
            Assert.IsTrue( bits > 0 );
            Assert.IsTrue( bits <= 8 );
            Assert.IsTrue( bits == 8 || ( value < ( 1 << bits ) ) );
            m_writer.WriteBits( value, bits );
        }

        public void SerializeBits( ushort value, int bits )
        {
            Assert.IsTrue( bits > 0 );
            Assert.IsTrue( bits <= 16 );
            Assert.IsTrue( bits == 16 || ( value < ( 1 << bits ) ) );
            m_writer.WriteBits( value, bits );
        }

        public void SerializeBits( uint value, int bits )
        {
            Assert.IsTrue( bits > 0 );
            Assert.IsTrue( bits <= 32 );
            Assert.IsTrue( bits == 32 || ( value < ( 1 << bits ) ) );
            m_writer.WriteBits( value, bits );
        }

        public void SerializeBits( ulong value, int bits )
        {
            Assert.IsTrue( bits > 0 );
            Assert.IsTrue( bits <= 64 );
            Assert.IsTrue( bits == 64 || ( value < ( 1UL << bits ) ) );

            uint loword = (uint) value;
            uint hiword = (uint) ( value >> 32 );

            if ( bits <= 32 )
            {
                m_writer.WriteBits( loword, bits );
            }
            else
            {
                m_writer.WriteBits( loword, 32 );
                m_writer.WriteBits( hiword, bits - 32 );
            }
        }

        public void SerializeBytes( byte[] data, int bytes )
        {
            Assert.IsTrue( data != null );
            Assert.IsTrue( bytes >= 0 );
            SerializeAlign();
            m_writer.WriteBytes( data, bytes );
        }

        public void SerializeString( string s )
        {
            SerializeAlign();
            int stringLength = (int) s.Length;
            Assert.IsTrue( stringLength <= Network.Constants.MaxStringLength );
            m_writer.WriteBits( (byte) stringLength, Util.BitsRequired( 0, Network.Constants.MaxStringLength ) );
            for ( int i = 0; i < stringLength; ++i )
            {
                char charValue = s[i];
                m_writer.WriteBits( charValue, 16 );
            }
        }

        public void SerializeFloat( float f )
        {
            byte[] byteArray = BitConverter.GetBytes( f );
            for ( int i = 0; i < 4; ++i )
                m_writer.WriteBits( byteArray[i], 8 );
        }

        public void SerializeAlign()
        {
            m_writer.WriteAlign();
        }

        public void Finish()
        {
            m_writer.Finish();
        }

        public int GetAlignBits()
        {
            return m_writer.GetAlignBits();
        }

        public byte[] GetData()
        {
            return m_writer.GetData();
        }

        public int GetBytesProcessed()
        {
            return m_writer.GetBytesWritten();
        }

        public int GetBitsProcessed()
        {
            return m_writer.GetBitsWritten();
        }

        public int GetError()
        {
            return m_error;
        }
    }

    public class ReadStream
    {
        BitReader m_reader = new BitReader();
        int m_bitsRead = 0;
        int m_error = Constants.STREAM_ERROR_NONE;
        byte[] m_floatBytes = new byte[4];

        public void Start( byte[] data )
        {
            m_reader.Start( data );
        }

        public bool SerializeSignedInteger( out int value, int min, int max )
        {
            Assert.IsTrue( min < max );
            int bits = Util.BitsRequired( min, max );
            if ( m_reader.WouldOverflow( bits ) )
            {
                m_error = Constants.STREAM_ERROR_OVERFLOW;
                value = 0;
                return false;
            }
            uint unsigned_value = m_reader.ReadBits( bits );
            value = (int) ( unsigned_value + min );
            m_bitsRead += bits;
            return true;
        }

        public bool SerializeUnsignedInteger( out uint value, uint min, uint max )
        {
            Assert.IsTrue( min < max );
            int bits = Util.BitsRequired( min, max );
            if ( m_reader.WouldOverflow( bits ) )
            {
                m_error = Constants.STREAM_ERROR_OVERFLOW;
                value = 0;
                return false;
            }
            uint unsigned_value = m_reader.ReadBits( bits );
            value = unsigned_value + min;
            m_bitsRead += bits;
            return true;
        }

        public bool SerializeBits( out byte value, int bits )
        {
            Assert.IsTrue( bits > 0 );
            Assert.IsTrue( bits <= 8 );
            if ( m_reader.WouldOverflow( bits ) )
            {
                m_error = Constants.STREAM_ERROR_OVERFLOW;
                value = 0;
                return false;
            }
            byte read_value = (byte) m_reader.ReadBits( bits );
            value = read_value;
            m_bitsRead += bits;
            return true;
        }

        public bool SerializeBits( out ushort value, int bits )
        {
            Assert.IsTrue( bits > 0 );
            Assert.IsTrue( bits <= 16 );
            if ( m_reader.WouldOverflow( bits ) )
            {
                m_error = Constants.STREAM_ERROR_OVERFLOW;
                value = 0;
                return false;
            }
            ushort read_value = (ushort) m_reader.ReadBits( bits );
            value = read_value;
            m_bitsRead += bits;
            return true;
        }

        public bool SerializeBits( out uint value, int bits )
        {
            Assert.IsTrue( bits > 0 );
            Assert.IsTrue( bits <= 32 );
            if ( m_reader.WouldOverflow( bits ) )
            {
                m_error = Constants.STREAM_ERROR_OVERFLOW;
                value = 0;
                return false;
            }
            uint read_value = m_reader.ReadBits( bits );
            value = read_value;
            m_bitsRead += bits;
            return true;
        }

        public bool SerializeBits( out ulong value, int bits )
        {
            Assert.IsTrue( bits > 0 );
            Assert.IsTrue( bits <= 64 );

            if ( m_reader.WouldOverflow( bits ) )
            {
                m_error = Constants.STREAM_ERROR_OVERFLOW;
                value = 0;
                return false;
            }

            if ( bits <= 32 )
            {
                uint loword = m_reader.ReadBits( bits );
                value = (ulong) loword;
            }
            else
            {
                uint loword = m_reader.ReadBits( 32 );
                uint hiword = m_reader.ReadBits( bits - 32 );
                value = ( (ulong) loword ) | ( ( (ulong) hiword ) << 32 );
            }

            return true;
        }

        public bool SerializeBytes( byte[] data, int bytes )
        {
            if ( !SerializeAlign() )
                return false;
            if ( m_reader.WouldOverflow( bytes * 8 ) )
            {
                m_error = Constants.STREAM_ERROR_OVERFLOW;
                return false;
            }
            m_reader.ReadBytes( data, bytes );
            m_bitsRead += bytes * 8;
            return true;
        }

        public bool SerializeString( out string s )
        {
            if ( !SerializeAlign() )
            {
                s = null;
                return false;
            }

            int stringLength;
            if ( !SerializeSignedInteger( out stringLength, 0, Network.Constants.MaxStringLength ) )
            {
                s = null;
                return false;
            }

            if ( m_reader.WouldOverflow( (int) ( stringLength * 16 ) ) )
            {
                m_error = Constants.STREAM_ERROR_OVERFLOW;
                s = null;
                return false;
            }

            char[] stringData = new char[Network.Constants.MaxStringLength];
            
            for ( int i = 0; i < stringLength; ++i )
            {
                stringData[i] = (char) m_reader.ReadBits( 16 );
            }
            
            s = new string( stringData, 0, stringLength );
            
            return true;
        }

        public bool SerializeFloat( out float f )
        {
            if ( m_reader.WouldOverflow( 32 ) )
            {
                m_error = Constants.STREAM_ERROR_OVERFLOW;
                f = 0.0f;
                return false;
            }

            for ( int i = 0; i < 4; ++i )
                m_floatBytes[i] = (byte) m_reader.ReadBits( 8 );

            f = BitConverter.ToSingle( m_floatBytes, 0 );

            return true;
        }

        public bool SerializeAlign()
        {
            int alignBits = m_reader.GetAlignBits();
            if ( m_reader.WouldOverflow( alignBits ) )
            {
                m_error = Constants.STREAM_ERROR_OVERFLOW;
                return false;
            }
            if ( !m_reader.ReadAlign() )
            {
                m_error = Constants.STREAM_ERROR_ALIGNMENT;
                return false;
            }
            m_bitsRead += alignBits;
            return true;
        }

        public void Finish()
        {
            m_reader.Finish();
        }

        public int GetAlignBits()
        {
            return m_reader.GetAlignBits();
        }

        public int GetBitsProcessed()
        {
            return m_bitsRead;
        }

        public int GetBytesProcessed()
        {
            return ( m_bitsRead + 7 ) / 8;
        }

        public int GetError()
        {
            return m_error;
        }
    }

    public class SerializeException : Exception
    {
        public SerializeException() { }
    };

    public class Serializer
    {
        // write

        public void write_bool( WriteStream stream, bool value )
        {
            uint unsigned_value = ( value == true ) ? 1U : 0U;
            stream.SerializeBits( unsigned_value, 1 );
        }

        public void write_int( WriteStream stream, int value, int min, int max )
        {
            stream.SerializeSignedInteger( value, min, max );
        }

        public void write_uint( WriteStream stream, uint value, uint min, uint max )
        {
            stream.SerializeUnsignedInteger( value, min, max );
        }

        public void write_bits( WriteStream stream, byte value, int bits )
        {
            stream.SerializeBits( value, bits );
        }

        public void write_bits( WriteStream stream, ushort value, int bits )
        {
            stream.SerializeBits( value, bits );
        }

        public void write_bits( WriteStream stream, uint value, int bits )
        {
            stream.SerializeBits( value, bits );
        }

        public void write_bits( WriteStream stream, ulong value, int bits )
        {
            stream.SerializeBits( value, bits );
        }

        public void write_string( WriteStream stream, string value )
        {
            stream.SerializeString( value );
        }

        public void write_float( WriteStream stream, float value )
        {
            stream.SerializeFloat( value );
        }

        // read

        public void read_bool( ReadStream stream, out bool value )
        {
            uint unsigned_value;
            if ( !stream.SerializeBits( out unsigned_value, 1 ) )
                throw new SerializeException();
            value = ( unsigned_value == 1 ) ? true : false;
        }

        public void read_int( ReadStream stream, out int value, int min, int max )
        {
            if ( !stream.SerializeSignedInteger( out value, min, max ) )
                throw new SerializeException();
        }

        public void read_uint( ReadStream stream, out uint value, uint min, uint max )
        {
            if ( !stream.SerializeUnsignedInteger( out value, min, max ) )
                throw new SerializeException();
        }

        public void read_bits( ReadStream stream, out byte value, int bits )
        {
            if ( !stream.SerializeBits( out value, bits ) )
                throw new SerializeException();
        }

        public void read_bits( ReadStream stream, out ushort value, int bits )
        {
            if ( !stream.SerializeBits( out value, bits ) )
                throw new SerializeException();
        }

        public void read_bits( ReadStream stream, out uint value, int bits )
        {
            if ( !stream.SerializeBits( out value, bits ) )
                throw new SerializeException();
        }

        public void read_bits( ReadStream stream, out ulong value, int bits )
        {
            if ( !stream.SerializeBits( out value, bits ) )
                throw new SerializeException();
        }

        public void read_string( ReadStream stream, out string value )
        {
            if ( !stream.SerializeString( out value ) )
                throw new SerializeException();
        }

        public void read_float( ReadStream stream, out float value )
        {
            if ( !stream.SerializeFloat( out value ) )
                throw new SerializeException();
        }
    }

    public class SequenceBuffer<T>
    {
        public T[] m_entries;
        uint[] m_entry_sequence;
        int m_size;
        ushort m_sequence;

        public T[] Entries
        {
            get
            {
                return m_entries;
            }
        }

        public SequenceBuffer( int size )
        {
            Assert.IsTrue( size > 0 );
            m_size = size;
            m_sequence = 0;
            m_entry_sequence = new uint[size];
            m_entries = new T[size];
            Reset();
        }

        public void Reset()
        {
            m_sequence = 0;
            for ( int i = 0; i < m_size; ++i )
            {
                m_entry_sequence[i] = 0xFFFFFFFF;
            }
        }

        public int Insert( ushort sequence )
        {
            if ( Util.SequenceGreaterThan( (ushort) ( sequence + 1 ), m_sequence ) )
            {
                RemoveEntries( m_sequence, sequence );

                m_sequence = (ushort) ( sequence + 1 );
            }
            else if ( Util.SequenceLessThan( sequence, (ushort) ( m_sequence - m_size ) ) )
            {
                return -1;
            }

            int index = sequence % m_size;

            m_entry_sequence[index] = sequence;

            return index;
        }

        public void Remove( ushort sequence )
        {
            m_entry_sequence[ sequence % m_size ] = 0xFFFFFFFF;
        }

        public bool Available( ushort sequence )
        {
            return m_entry_sequence[ sequence % m_size ] == 0xFFFFFFFF;
        }

        public bool Exists( ushort sequence )
        {
            return m_entry_sequence[ sequence % m_size ] == (uint) sequence;
        }
       
        public int Find( ushort sequence )
        {
            int index = sequence % m_size;
            if ( m_entry_sequence[index] == (uint)sequence )
                return index;
            else
                return -1;
        }

        public ushort GetSequence()
        {
            return m_sequence;
        }

        public int GetSize()
        {
            return m_size;
        }

        public void RemoveEntries( ushort start_sequence, ushort finish_sequence )
        {
            int start_sequence_int = (int) start_sequence;
            int finish_sequence_int = (int) finish_sequence;
            if ( finish_sequence_int < start_sequence_int ) 
                finish_sequence_int += 65535;
            for ( int sequence = start_sequence_int; sequence <= finish_sequence_int; ++sequence )
                m_entry_sequence[sequence % m_size] = 0xFFFFFFFF;
        }
    }

    public class SequenceBuffer32<T> where T : new()
    {
        public T[] m_entries;
        uint[] m_entry_sequence;
        int m_size;
        uint m_sequence;

        public T[] Entries
        {                              
            get
            {
                return m_entries;
            }
        }

        public SequenceBuffer32( int size )
        {
            Assert.IsTrue( size > 0 );
            m_size = size;
            m_sequence = 0;
            m_entry_sequence = new uint[size];
            m_entries = new T[size];
            for ( int i = 0; i < size; ++i )
                m_entries[i] = new T();
            Reset();
        }

        public void Reset()
        {
            m_sequence = 0;
            for ( int i = 0; i < m_size; ++i )
            {
                m_entry_sequence[i] = 0xFFFFFFFF;
            }
        }

        public int Insert( uint sequence )
        {
            Assert.IsTrue( sequence != 0xFFFFFFFF );

            if ( sequence + 1 > m_sequence )
            {
                RemoveEntries( m_sequence, sequence );

                m_sequence = sequence + 1;
            }
            else if ( sequence < m_sequence - m_size )
            {
                return -1;
            }

            int index = (int) ( sequence % m_size );

            m_entry_sequence[index] = sequence;

            return index;
        }

        public void Remove( uint sequence )
        {
            Assert.IsTrue( sequence != 0xFFFFFFFF );
            m_entry_sequence[sequence % m_size] = 0xFFFFFFFF;
        }

        public bool Available( uint sequence )
        {
            Assert.IsTrue( sequence != 0xFFFFFFFF );
            return m_entry_sequence[sequence % m_size] == 0xFFFFFFFF;
        }

        public bool Exists( uint sequence )
        {
            Assert.IsTrue( sequence != 0xFFFFFFFF );
            return m_entry_sequence[sequence % m_size] == sequence;
        }

        public int Find( uint sequence )
        {
            Assert.IsTrue( sequence != 0xFFFFFFFF );
            int index = (int) ( sequence % m_size );
            if ( m_entry_sequence[index] == sequence )
                return index;
            else
                return -1;
        }

        public uint GetSequence()
        {
            return m_sequence;
        }

        public int GetSize()
        {
            return m_size;
        }

        public void RemoveEntries( uint start_sequence, uint finish_sequence )
        {
            Assert.IsTrue( start_sequence <= finish_sequence );

            if ( finish_sequence - start_sequence < m_size )
            {
                for ( uint sequence = start_sequence; sequence <= finish_sequence; ++sequence )
                    m_entry_sequence[sequence % m_size] = 0xFFFFFFFF;
            }
            else
            {
                for ( int i = 0; i < m_size; ++i )
                {
                    m_entry_sequence[i] = 0xFFFFFFFF;
                }
            }
        }
    }

    public struct PacketHeader
    {
        public ushort sequence;
        public ushort ack;
        public uint ack_bits;
        public uint frameNumber;                    // physics simulation frame # for jitter buffer
        public ushort resetSequence;                // incremented each time the simulation is reset
        public float avatarSampleTimeOffset;        // offset between the current physics frame time of this packet and the time where the avatar state was sampled
    }

    public struct SentPacketData 
    { 
        public bool acked;
    };

    public struct ReceivedPacketData {}

    public class Connection
    {
        public const int MaximumAcks = 1024;
        public const int SentPacketsSize = 1024;
        public const int ReceivedPacketsSize = 1024;

        ushort m_sequence = 0;
        int m_numAcks = 0;
        ushort[] m_acks = new ushort[MaximumAcks];
        SequenceBuffer<SentPacketData> m_sentPackets = new SequenceBuffer<SentPacketData>( SentPacketsSize );
        SequenceBuffer<ReceivedPacketData> m_receivedPackets = new SequenceBuffer<ReceivedPacketData>( ReceivedPacketsSize );

        public void GeneratePacketHeader( out PacketHeader header )
        {
            header.sequence = m_sequence;
            Util.GenerateAckBits( m_receivedPackets, out header.ack, out header.ack_bits );
            header.frameNumber = 0;
            header.resetSequence = 0;
            header.avatarSampleTimeOffset = 0.0f;
            int index = m_sentPackets.Insert( m_sequence );
            Assert.IsTrue( index != -1 );
            m_sentPackets.Entries[index].acked = false;
            m_sequence++;
        }

        public void ProcessPacketHeader( ref PacketHeader header )
        {
            PacketReceived( header.sequence );

            for ( int i = 0; i < 32; ++i )
            {
                if ( ( header.ack_bits & 1 ) != 0 )
                {                    
                    ushort acked_sequence = (ushort) ( header.ack - i );
                    int index = m_sentPackets.Find( acked_sequence );
                    if ( index != -1 && !m_sentPackets.Entries[index].acked )
                    {
                        PacketAcked( acked_sequence );
                        m_sentPackets.Entries[index].acked = true;
                    }
                }
                header.ack_bits >>= 1;
            }
        }

        public void GetAcks( ref ushort[] acks, ref int numAcks )
        {
            numAcks = Math.Min( m_numAcks, acks.Length );
            for ( int i = 0; i < numAcks; ++i )
                acks[i] = m_acks[i];
            m_numAcks = 0;
        }

        public void Reset()
        {
            m_sequence = 0;
            m_numAcks = 0;
            m_sentPackets.Reset();
            m_receivedPackets.Reset();
        }

        void PacketReceived( ushort sequence )
        {
            m_receivedPackets.Insert( sequence );
        }

        void PacketAcked( ushort sequence )
        {
            if ( m_numAcks == MaximumAcks - 1 )
                return;
            m_acks[m_numAcks++] = sequence;
        }
    }

    public class Simulator
    {
        System.Random random = new System.Random();

        float m_latency;                                // latency in milliseconds
        float m_jitter;                                 // jitter in milliseconds +/-
        float m_packetLoss;                             // packet loss percentage
        float m_duplicate;                              // duplicate packet percentage
        double m_time;                                  // current time from last call to advance time. initially 0.0
        int m_insertIndex;                              // current index in the packet entry array. new packets are inserted here.
        int m_receiveIndex;                             // current receive index. packets entries are walked until this wraps back around to m_insertInsdex.

        struct PacketEntry
        {
            public int from;                            // address this packet is from
            public int to;                              // address this packet is sent to
            public double deliveryTime;                 // delivery time for this packet
            public byte[] packetData;                   // packet data
        };

        PacketEntry[] m_packetEntries;                  // packet entries

        public Simulator()
        {
            m_packetEntries = new PacketEntry[4*1024];
        }

        public void SetLatency( float milliseconds )
        {
            m_latency = milliseconds;
        }

        public void SetJitter( float milliseconds )
        {
            m_jitter = milliseconds;
        }

        public void SetPacketLoss( float percent )
        {
            m_packetLoss = percent;
        }

        public void SetDuplicate( float percent )
        {
            m_duplicate = percent;
        }

        public float RandomFloat( float min, float max )
        {
            return (float) random.NextDouble() * ( max - min ) + min;
        }

        public void SendPacket( int from, int to, byte[] packetData )
        {
            if ( RandomFloat( 0.0f, 100.0f ) <= m_packetLoss )
                return;

            double delay = m_latency / 1000.0;

            if ( m_jitter > 0 )
                delay += RandomFloat( 0, +m_jitter ) / 1000.0;

            m_packetEntries[m_insertIndex].from = from;
            m_packetEntries[m_insertIndex].to = to;
            m_packetEntries[m_insertIndex].packetData = packetData;
            m_packetEntries[m_insertIndex].deliveryTime = m_time + delay;

            m_insertIndex = ( m_insertIndex + 1 ) % m_packetEntries.Length;

            if ( RandomFloat( 0.0f, 100.0f ) <= m_duplicate )
            {
                byte[] duplicatePacketData = new byte[packetData.Length];
                Buffer.BlockCopy( packetData, 0, duplicatePacketData, 0, packetData.Length );

                m_packetEntries[m_insertIndex].from = from;
                m_packetEntries[m_insertIndex].to = to;
                m_packetEntries[m_insertIndex].packetData = packetData;
                m_packetEntries[m_insertIndex].deliveryTime = m_time + delay + RandomFloat( 0.0f, +1.0f );

                m_insertIndex = ( m_insertIndex + 1 ) % m_packetEntries.Length;
            }
        }

        public byte[] ReceivePacket( out int from, out int to )
        {
            while ( m_receiveIndex != m_insertIndex )
            {
                if ( m_packetEntries[m_receiveIndex].packetData != null && m_packetEntries[m_receiveIndex].deliveryTime <= m_time )
                {
                    var packetData = m_packetEntries[m_receiveIndex].packetData;
                    from = m_packetEntries[m_receiveIndex].from;
                    to = m_packetEntries[m_receiveIndex].to;
                    m_packetEntries[m_receiveIndex].packetData = null;
                    m_receiveIndex = ( m_receiveIndex + 1 ) % m_packetEntries.Length;
                    return packetData;
                }
                else
                {
                    m_receiveIndex = ( m_receiveIndex + 1 ) % m_packetEntries.Length;
                }
            }

            from = 0;
            to = 0;
            return null;
        }

        public void AdvanceTime( double time )
        {
            m_time = time;
            m_receiveIndex = ( m_insertIndex + 1 ) % m_packetEntries.Length;
        }
    }
}
