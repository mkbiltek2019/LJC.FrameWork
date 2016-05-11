﻿using System;
using System.Collections.Generic;
using System.Text;

namespace LJC.FrameWork.Net.IMAP.Server
{
    /// <summary>
    /// This class provides data for <b cref="IMAP_Session.SetAcl">IMAP_Session.SetAcl</b> event.
    /// </summary>
    public class IMAP_e_SetAcl : EventArgs
    {
        private IMAP_r_ServerStatus m_pResponse  = null;
        private string              m_Folder     = null;
        private string              m_Identifier = null;
        private IMAP_Flags_SetType  m_SetType    = IMAP_Flags_SetType.Replace;
        private string              m_Rights     = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="folder">Folder name with optional path.</param>
        /// <param name="identifier">ACL identifier (normally user or group name).</param>
        /// <param name="flagsSetType">Flags set type.</param>
        /// <param name="rights">Identifier rights.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b>,<b>identifier</b>,<b>rights</b> or <b>response</b> is null reference.</exception>
        internal IMAP_e_SetAcl(string folder,string identifier,IMAP_Flags_SetType flagsSetType,string rights,IMAP_r_ServerStatus response)
        {
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(identifier == null){
                throw new ArgumentNullException("identifier");
            }
            if(rights == null){
                throw new ArgumentNullException("rights");
            }
            if(response == null){
                throw new ArgumentNullException("response");
            }

            m_pResponse  = response;
            m_Folder     = folder;
            m_Identifier = identifier;
            m_SetType    = flagsSetType;
            m_Rights     = rights;
        }


        #region Properties implementation

        /// <summary>
        /// Gets or sets IMAP server response to this operation.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when null reference value set.</exception>
        public IMAP_r_ServerStatus Response
        {
            get{ return m_pResponse; }

            set{ 
                if(value == null){
                    throw new ArgumentNullException("value");
                }

                m_pResponse = value; 
            }
        }

        /// <summary>
        /// Gets folder name with optional path.
        /// </summary>
        public string Folder
        {
            get{ return m_Folder; }
        }

        /// <summary>
        /// Gets ACL identifier (normally user or group name).
        /// </summary>
        public string Identifier
        {
            get{ return m_Identifier; }
        }

        /// <summary>
        /// Gets flags set type.
        /// </summary>
        public IMAP_Flags_SetType FlagsSetType
        {
            get{ return m_SetType; }
        }

        /// <summary>
        /// Gets identifier rights.
        /// </summary>
        public string Rights
        {
            get{ return m_Rights; }
        }

        #endregion
    }
}
