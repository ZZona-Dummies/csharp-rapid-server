﻿using System.Collections.Generic;
using System.Net.Sockets;

namespace RapidServer.Http.Type2
{
    // '' <summary>
    // '' A shared buffer which the SocketAsyncEventArgsPool utilizes to read/write data without memory thrashing/fragmentation.
    // '' </summary>
    // '' <remarks></remarks>
    internal class BufferManager
    {
        private int m_numBytes;

        private byte[] m_buffer;

        private Stack<int> m_freeIndexPool;

        private int m_currentIndex;

        private int m_bufferSize;

        public BufferManager(int totalBytes, int bufferSize)
        {
            m_numBytes = totalBytes;
            m_currentIndex = 0;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
        }

        //  Allocates buffer space used by the buffer pool
        public void InitBuffer()
        {
            //  create one big large buffer and divide that
            //  out to each SocketAsyncEventArg object
            m_buffer = new byte[] {
                    (byte)(m_numBytes - 1)};
        }

        //  Assigns a buffer from the buffer pool to thespecified SocketAsyncEventArgs object returns true if the buffer was successfully set, else false.
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (m_freeIndexPool.Count > 0)
                args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), m_bufferSize);
            else
            {
                if (m_numBytes - m_bufferSize < m_currentIndex)
                    return false;

                args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);
                m_currentIndex = (m_currentIndex + m_bufferSize);
            }

            return true;
        }

        //  Removes the buffer from a SocketAsyncEventArg object. This frees the buffer back to the buffer pool.
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            m_freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }

    // '' <summary>
    // '' A client state object representing a single client.
    // '' </summary>
    // '' <remarks></remarks>
    public class AsyncUserToken
    {
        private Socket _socket;

        public string Content;

        public AsyncUserToken()
        {
        }

        public AsyncUserToken(Socket socket)
        {
            _socket = socket;
        }

        public Socket Socket
        {
            get
            {
                return _socket;
            }
            set
            {
                _socket = value;
            }
        }
    }
}