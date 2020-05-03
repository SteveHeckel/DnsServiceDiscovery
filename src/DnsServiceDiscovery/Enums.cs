using System;
using System.Diagnostics.CodeAnalysis;

namespace Mittosoft.DnsServiceDiscovery
{
    /* Possible protocol values */
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ProtocolFlags : uint
    {
        None = 0,

        /* for DNSServiceGetAddrInfo() */
        IPv4 = 0x01,
        IPv6 = 0x02,
        /* 0x04 and 0x08 reserved for future internetwork protocols */

        IPv4v6 = IPv4 | IPv6,

        /* for DNSServiceNATPortMappingCreate() */
        UDP = 0x10,
        TCP = 0x20
        /* 0x40 and 0x80 reserved for future transport protocols, e.g. SCTP [RFC 2960]
         * or DCCP [RFC 4340]. If future NAT gateways are created that support port
         * mappings for these protocols, new constants will be defined here.
         */
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ResourceRecordType : ushort
    {
        None,
        A          = 1,      /* Host address. */
        NS         = 2,      /* Authoritative server. */
        MD         = 3,      /* Mail destination. */
        MF         = 4,      /* Mail forwarder. */
        CNAME      = 5,      /* Canonical name. */
        SOA        = 6,      /* Start of authority zone. */
        MB         = 7,      /* Mailbox domain name. */
        MG         = 8,      /* Mail group member. */
        MR         = 9,      /* Mail rename name. */
        NULL       = 10,     /* Null resource record. */
        WKS        = 11,     /* Well known service. */
        PTR        = 12,     /* Domain name pointer. */
        HINFO      = 13,     /* Host information. */
        MINFO      = 14,     /* Mailbox information. */
        MX         = 15,     /* Mail routing information. */
        TXT        = 16,     /* One or more text strings (NOT "zero or more..."). */
        RP         = 17,     /* Responsible person. */
        AFSDB      = 18,     /* AFS cell database. */
        X25        = 19,     /* X_25 calling address. */
        ISDN       = 20,     /* ISDN calling address. */
        RT         = 21,     /* Router. */
        NSAP       = 22,     /* NSAP address. */
        NSAP_PTR   = 23,     /* Reverse NSAP lookup (deprecated). */
        SIG        = 24,     /* Security signature. */
        KEY        = 25,     /* Security key. */
        PX         = 26,     /* X.400 mail mapping. */
        GPOS       = 27,     /* Geographical position (withdrawn). */
        AAAA       = 28,     /* IPv6 Address. */
        LOC        = 29,     /* Location Information. */
        NXT        = 30,     /* Next domain (security). */
        EID        = 31,     /* Endpoint identifier. */
        NIMLOC     = 32,     /* Nimrod Locator. */
        SRV        = 33,     /* Server Selection. */
        ATMA       = 34,     /* ATM Address */
        NAPTR      = 35,     /* Naming Authority PoinTeR */
        KX         = 36,     /* Key Exchange */
        CERT       = 37,     /* Certification record */
        A6         = 38,     /* IPv6 Address (deprecated) */
        DNAME      = 39,     /* Non-terminal DNAME (for IPv6) */
        SINK       = 40,     /* Kitchen sink (experimental) */
        OPT        = 41,     /* EDNS0 option (meta-RR) */
        APL        = 42,     /* Address Prefix List */
        DS         = 43,     /* Delegation Signer */
        SSHFP      = 44,     /* SSH Key Fingerprint */
        IPSECKEY   = 45,     /* IPSECKEY */
        RRSIG      = 46,     /* RRSIG */
        NSEC       = 47,     /* Denial of Existence */
        DNSKEY     = 48,     /* DNSKEY */
        DHCID      = 49,     /* DHCP Client Identifier */
        NSEC3      = 50,     /* Hashed Authenticated Denial of Existence */
        NSEC3PARAM = 51,     /* Hashed Authenticated Denial of Existence */

        HIP        = 55,     /* Host Identity Protocol */

        SPF        = 99,     /* Sender Policy Framework for E-Mail */
        UINFO      = 100,    /* IANA-Reserved */
        UID        = 101,    /* IANA-Reserved */
        GID        = 102,    /* IANA-Reserved */
        UNSPEC     = 103,    /* IANA-Reserved */

        TKEY       = 249,    /* Transaction key */
        TSIG       = 250,    /* Transaction signature. */
        IXFR       = 251,    /* Incremental zone transfer. */
        AXFR       = 252,    /* Transfer zone of authority. */
        MAILB      = 253,    /* Transfer mailbox records. */
        MAILA      = 254,    /* Transfer mail agent records. */
        Any        = 255     /* Wildcard match. */
    }

    internal enum OperationCode : uint
    {
        None = 0,               // No request yet received on this connection
        ConnectionRequest = 1,  // connected socket via DNSServiceConnect()
        RegisterRecordRequest,  // register/remove record only valid for connected sockets
        RemoveRecordRequest,
        EnumerationRequest,
        RegisterServiceRequest,
        BrowseRequest,
        ResolveRequest,
        QueryRequest,
        ReconfirmRecordRequest,
        AddRecordRequest,
        UpdateRecordRequest,
        SetDomainRequest,       // Up to here is in Tiger and B4W 1.0.3
        GetPropertyRequest,     // New in B4W 1.0.4
        PortMappingRequest,     // New in Leopard and B4W 2.0
        AddressInfoRequest,
        SendBpf,                // New in SL
        GetPidRequest,
        ReleaseRequest,
        ConnectionDelegateRequest,
        CancelRequest = 63,
        //
        // Reply operation codes
        //
        EnumerationReply = 64,
        RegisterServiceReply,
        BrowseReply,
        ResolveReply,
        QueryReply,
        RegisterRecordReply,    // Up to here is in Tiger and B4W 1.0.3
        GetpropertyReply,       // New in B4W 1.0.4
        PortMappingReply,       // New in Leopard and B4W 2.0
        AddressInfoReply
    }

    public enum ServiceError : int
    {
        NoError = 0,
        Unknown = -65537,                   /* 0xFFFE FFFF */
        NoSuchName = -65538,
        NoMemory = -65539,
        BadParam = -65540,
        BadReference = -65541,
        BadState = -65542,
        BadFlags = -65543,
        Unsupported = -65544,
        NotInitialized = -65545,
        AlreadyRegistered = -65547,
        NameConflict = -65548,
        Invalid = -65549,
        Firewall = -65550,
        Incompatible = -65551,              /* client library incompatible with daemon */
        BadInterfaceIndex = -65552,
        Refused = -65553,
        NoSuchRecord = -65554,
        NoAuth = -65555,
        NoSuchKey = -65556,
        NatTraversal = -65557,
        DoubleNat = -65558, 
        BadTime = -65559,                   /* Codes up to here existed in Tiger */
        BadSig = -65560,
        BadKey = -65561,
        Transient = -65562,
        ServiceNotRunning = -65563,         /* Background daemon not running */
        NatPortMappingUnsupported = -65564, /* NAT doesn't support PCP, NAT-PMP or UPnP */
        NatPortMappingDisabled = -65565,    /* NAT supports PCP, NAT-PMP or UPnP, but it's disabled by the administrator */
        NoRouter = -65566,                  /* No router currently configured (probably no network connectivity) */
        PollingMode = -65567,
        Timeout = -65568

        /* mDNS Error codes are in the range
         * FFFE FF00 (-65792) to FFFE FFFF (-65537) */
    };

    /*! @enum General flags
     * Most DNS-SD API functions and callbacks include a DNSServiceFlags parameter.
     * As a general rule, any given bit in the 32-bit flags field has a specific fixed meaning,
     * regardless of the function or callback being used. For any given function or callback,
     * typically only a subset of the possible flags are meaningful, and all others should be zero.
     * The discussion section for each API call describes which flags are valid for that call
     * and callback. In some cases, for a particular call, it may be that no flags are currently
     * defined, in which case the DNSServiceFlags parameter exists purely to allow future expansion.
     * In all cases, developers should expect that in future releases, it is possible that new flag
     * values will be defined, and write code with this in mind. For example, code that tests
     *     if (flags == kDNSServiceFlagsAdd) ...
     * will fail if, in a future release, another bit in the 32-bit flags field is also set.
     * The reliable way to test whether a particular bit is set is not with an equality test,
     * but with a bitwise mask:
     *     if (flags & kDNSServiceFlagsAdd) ...
     * With the exception of kDNSServiceFlagsValidate, each flag can be valid(be set) 
     * EITHER only as an input to one of the DNSService*() APIs OR only as an output
     * (provide status) through any of the callbacks used. For example, kDNSServiceFlagsAdd
     * can be set only as an output in the callback, whereas the kDNSServiceFlagsIncludeP2P
     * can be set only as an input to the DNSService*() APIs. See comments on kDNSServiceFlagsValidate  
     * defined in enum below.
     */
    [Flags]
    internal enum ServiceFlags : uint
    {
        None,
        MoreComing = 0x1,
        /* MoreComing indicates to a callback that at least one more result is
         * queued and will be delivered following immediately after this one.
         * When the MoreComing flag is set, applications should not immediately
         * update their UI, because this can result in a great deal of ugly flickering
         * on the screen, and can waste a great deal of CPU time repeatedly updating
         * the screen with content that is then immediately erased, over and over.
         * Applications should wait until MoreComing is not set, and then
         * update their UI when no more changes are imminent.
         * When MoreComing is not set, that doesn't mean there will be no more
         * answers EVER, just that there are no more answers immediately
         * available right now at this instant. If more answers become available
         * in the future they will be delivered as usual.
         */

        AutoTrigger = 0x1,
        /* Valid for browses using kDNSServiceInterfaceIndexAny.
         * Will auto trigger the browse over AWDL as well once the service is discoveryed
         * over BLE.
         * This flag is an input value to DNSServiceBrowse(), which is why we can
         * use the same value as MoreComing, which is an output flag
         * for various client callbacks.
        */

        Add = 0x2,
        Default = 0x4,
        /* Flags for domain enumeration and browse/query reply callbacks.
         * "Default" applies only to enumeration and is only valid in
         * conjunction with "Add". An enumeration callback with the "Add"
         * flag NOT set indicates a "Remove", i.e. the domain is no longer
         * valid.
         */

        NoAutoRename = 0x8,
        /* Flag for specifying renaming behavior on name conflict when registering
         * non-shared records. By default, name conflicts are automatically handled
         * by renaming the service. NoAutoRename overrides this behavior - with this
         * flag set, name conflicts will result in a callback. The NoAutorename flag
         * is only valid if a name is explicitly specified when registering a service
         * (i.e. the default name is not used.)
         */

        Shared = 0x10,
        Unique = 0x20,
        /* Flag for registering individual records on a connected
         * DNSServiceRef. Shared indicates that there may be multiple records
         * with this name on the network (e.g. PTR records). Unique indicates that the
         * record's name is to be unique on the network (e.g. SRV records).
         */

        BrowseDomains = 0x40,
        RegistrationDomains = 0x80,
        /* Flags for specifying domain enumeration type in DNSServiceEnumerateDomains.
         * BrowseDomains enumerates domains recommended for browsing, RegistrationDomains
         * enumerates domains recommended for registration.
         */

        LongLivedQuery = 0x100,
        /* Flag for creating a long-lived unicast query for the DNSServiceQueryRecord call. */

        AllowRemoteQuery = 0x200,
        /* Flag for creating a record for which we will answer remote queries
         * (queries from hosts more than one hop away; hosts not directly connected to the local link).
         */

        ForceMulticast = 0x400,
        /* Flag for signifying that a query or registration should be performed exclusively via multicast
         * DNS, even for a name in a domain (e.g. foo.apple.com.) that would normally imply unicast DNS.
         */

        Force = 0x800,    // This flag is deprecated.

        KnownUnique = 0x800,
        /* 
         * Client guarantees that record names are unique, so we can skip sending out initial
         * probe messages.  Standard name conflict resolution is still done if a conflict is discovered.
         * Currently only valid for a DNSServiceRegister call.
         */

        ReturnIntermediates = 0x1000,
        /* Flag for returning intermediate results.
         * For example, if a query results in an authoritative NXDomain (name does not exist)
         * then that result is returned to the client. However the query is not implicitly
         * cancelled -- it remains active and if the answer subsequently changes
         * (e.g. because a VPN tunnel is subsequently established) then that positive
         * result will still be returned to the client.
         * Similarly, if a query results in a CNAME record, then in addition to following
         * the CNAME referral, the intermediate CNAME result is also returned to the client.
         * When this flag is not set, NXDomain errors are not returned, and CNAME records
         * are followed silently without informing the client of the intermediate steps.
         * (In earlier builds this flag was briefly calledReturnCNAME)
         */

        NonBrowsable = 0x2000,
        /* A service registered with the NonBrowsable flag set can be resolved using
         * DNSServiceResolve(), but will not be discoverable using DNSServiceBrowse().
         * This is for cases where the name is actually a GUID; it is found by other means;
         * there is no end-user benefit to browsing to find a long list of opaque GUIDs.
         * Using the NonBrowsable flag creates SRV+TXT without the cost of also advertising
         * an associated PTR record.
         */

        ShareConnection = 0x4000,
        /* For efficiency, clients that perform many concurrent operations may want to use a
         * single Unix Domain Socket connection with the background daemon, instead of having a
         * separate connection for each independent operation. To use this mode, clients first
         * call DNSServiceCreateConnection(&MainRef) to initialize the main DNSServiceRef.
         * For each subsequent operation that is to share that same connection, the client copies
         * the MainRef, and then passes the address of that copy, setting the ShareConnection flag
         * to tell the library that this DNSServiceRef is not a typical uninitialized DNSServiceRef;
         * it's a copy of an existing DNSServiceRef whose connection information should be reused.
         *
         * For example:
         *
         * DNSServiceErrorType error;
         * DNSServiceRef MainRef;
         * error = DNSServiceCreateConnection(&MainRef);
         * if (error) ...
         * DNSServiceRef BrowseRef = MainRef;  // Important: COPY the primary DNSServiceRef first...
         * error = DNSServiceBrowse(&BrowseRef, ShareConnection, ...); // then use the copy
         * if (error) ...
         * ...
         * DNSServiceRefDeallocate(BrowseRef); // Terminate the browse operation
         * DNSServiceRefDeallocate(MainRef);   // Terminate the shared connection
         * Also see Point 4.(Don't Double-Deallocate if the MainRef has been Deallocated) in Notes below:
         *
         * Notes:
         *
         * 1. Collective MoreComing flag
         * When callbacks are invoked using a shared DNSServiceRef, the
         * MoreComing flag applies collectively to *all* active
         * operations sharing the same parent DNSServiceRef. If the MoreComing flag is
         * set it means that there are more results queued on this parent DNSServiceRef,
         * but not necessarily more results for this particular callback function.
         * The implication of this for client programmers is that when a callback
         * is invoked with the MoreComing flag set, the code should update its
         * internal data structures with the new result, and set a variable indicating
         * that its UI needs to be updated. Then, later when a callback is eventually
         * invoked with the MoreComing flag not set, the code should update *all*
         * stale UI elements related to that shared parent DNSServiceRef that need
         * updating, not just the UI elements related to the particular callback
         * that happened to be the last one to be invoked.
         *
         * 2. Canceling operations and MoreComing
         * Whenever you cancel any operation for which you had deferred UI updates
         * waiting because of a MoreComing flag, you should perform
         * those deferred UI updates. This is because, after cancelling the operation,
         * you can no longer wait for a callback *without* MoreComing set, to tell
         * you do perform your deferred UI updates (the operation has been canceled,
         * so there will be no more callbacks). An implication of the collective
         * MoreComing flag for shared connections is that this
         * guideline applies more broadly -- any time you cancel an operation on
         * a shared connection, you should perform all deferred UI updates for all
         * operations sharing that connection. This is because the MoreComing flag
         * might have been referring to events coming for the operation you canceled,
         * which will now not be coming because the operation has been canceled.
         *
         * 3. Only share DNSServiceRef's created with DNSServiceCreateConnection
         * Calling DNSServiceCreateConnection(&ref) creates a special shareable DNSServiceRef.
         * DNSServiceRef's created by other calls like DNSServiceBrowse() or DNSServiceResolve()
         * cannot be shared by copying them and using ShareConnection.
         *
         * 4. Don't Double-Deallocate if the MainRef has been Deallocated
         * Calling DNSServiceRefDeallocate(ref) for a particular operation's DNSServiceRef terminates
         * just that operation. Calling DNSServiceRefDeallocate(ref) for the main shared DNSServiceRef
         * (the parent DNSServiceRef, originally created by DNSServiceCreateConnection(&ref))
         * automatically terminates the shared connection and all operations that were still using it.
         * After doing this, DO NOT then attempt to deallocate any remaining subordinate DNSServiceRef's.
         * The memory used by those subordinate DNSServiceRef's has already been freed, so any attempt
         * to do a DNSServiceRefDeallocate (or any other operation) on them will result in accesses
         * to freed memory, leading to crashes or other equally undesirable results.
         *
         * 5. Thread Safety
         * The dns_sd.h API does not presuppose any particular threading model, and consequently
         * does no locking internally (which would require linking with a specific threading library).
         * If the client concurrently, from multiple threads (or contexts), calls API routines using 
         * the same DNSServiceRef, it is the client's responsibility to provide mutual exclusion for 
         * that DNSServiceRef.

         * For example, use of DNSServiceRefDeallocate requires caution. A common mistake is as follows:
         * Thread B calls DNSServiceRefDeallocate to deallocate sdRef while Thread A is processing events
         * using sdRef. Doing this will lead to intermittent crashes on thread A if the sdRef is used after
         * it was deallocated.

         * A telltale sign of this crash type is to see DNSServiceProcessResult on the stack preceding the
         * actual crash location.

         * To state this more explicitly, mDNSResponder does not queue DNSServiceRefDeallocate so
         * that it occurs discretely before or after an event is handled.
         */

        SuppressUnusable = 0x8000,
        /*
         * This flag is meaningful only in DNSServiceQueryRecord which suppresses unusable queries on the
         * wire. If "hostname" is a wide-area unicast DNS hostname (i.e. not a ".local." name)
         * but this host has no routable IPv6 address, then the call will not try to look up IPv6 addresses
         * for "hostname", since any addresses it found would be unlikely to be of any use anyway. Similarly,
         * if this host has no routable IPv4 address, the call will not try to look up IPv4 addresses for
         * "hostname".
         */

        Timeout = 0x10000,
        /*
         * When kDNServiceFlagsTimeout is passed to DNSServiceQueryRecord or DNSServiceGetAddrInfo, the query is
         * stopped after a certain number of seconds have elapsed. The time at which the query will be stopped
         * is determined by the system and cannot be configured by the user. The query will be stopped irrespective
         * of whether a response was given earlier or not. When the query is stopped, the callback will be called
         * with an error code of kDNSServiceErr_Timeout and a NULL sockaddr will be returned for DNSServiceGetAddrInfo
         * and zero length rdata will be returned for DNSServiceQueryRecord.
         */

        IncludeP2P = 0x20000,
        /*
         * Include P2P interfaces when kDNSServiceInterfaceIndexAny is specified.
         * By default, specifying kDNSServiceInterfaceIndexAny does not include P2P interfaces.
         */

        WakeOnResolve = 0x40000,
        /*
        * This flag is meaningful only in DNSServiceResolve. When set, it tries to send a magic packet
        * to wake up the client.
        */

        BackgroundTrafficClass = 0x80000,
        /*
        * This flag is meaningful for Unicast DNS queries. When set, it uses the background traffic 
        * class for packets that service the request.
        */

        IncludeAWDL = 0x100000,
        /*
         * Include AWDL interface when kDNSServiceInterfaceIndexAny is specified.
         */

        Validate = 0x200000,
        /*
         * This flag is meaningful in DNSServiceGetAddrInfo and DNSServiceQueryRecord. This is the ONLY flag to be valid 
         * as an input to the APIs and also an output through the callbacks in the APIs.
         *
         * When this flag is passed to DNSServiceQueryRecord and DNSServiceGetAddrInfo to resolve unicast names, 
         * the response  will be validated using DNSSEC. The validation results are delivered using the flags field in 
         * the callback and Validate is marked in the flags to indicate that DNSSEC status is also available.
         * When the callback is called to deliver the query results, the validation results may or may not be available. 
         * If it is not delivered along with the results, the validation status is delivered when the validation completes.
         * 
         * When the validation results are delivered in the callback, it is indicated by marking the flags with
         * Validate and Add along with the DNSSEC status flags (described below) and a NULL
         * sockaddr will be returned for DNSServiceGetAddrInfo and zero length rdata will be returned for DNSServiceQueryRecord.
         * DNSSEC validation results are for the whole RRSet and not just individual records delivered in the callback. When
         * Add is not set in the flags, applications should implicitly assume that the DNSSEC status of the 
         * RRSet that has been delivered up until that point is not valid anymore, till another callback is called with
         * Add and Validate.
         *
         * The following four flags indicate the status of the DNSSEC validation and marked in the flags field of the callback.
         * When any of the four flags is set, Validate will also be set. To check the validation status, the 
         * other applicable output flags should be masked. See kDNSServiceOutputFlags below.
         */

        Secure = 0x200010,
        /*
         * The response has been validated by verifying all the signatures in the response and was able to
         * build a successful authentication chain starting from a known trust anchor.   
         */

        Insecure = 0x200020,
        /*
         * A chain of trust cannot be built starting from a known trust anchor to the response.
         */

        Bogus = 0x200040,
        /*
         * If the response cannot be verified to be secure due to expired signatures, missing signatures etc.,
         * then the results are considered to be bogus.
         */

        Indeterminate = 0x200080,
        /*
         * There is no valid trust anchor that can be used to determine whether a response is secure or not.
         */

        UnicastResponse = 0x400000,
        /*
         * Request unicast response to query.
         */
        ValidateOptional = 0x800000,

        /*
         * This flag is identical to Validate except for the case where the response
         * cannot be validated. If this flag is set in DNSServiceQueryRecord or DNSServiceGetAddrInfo,
         * the DNSSEC records will be requested for validation. If they cannot be received for some reason
         * during the validation (e.g., zone is not signed, zone is signed but cannot be traced back to
         * root, recursive server does not understand DNSSEC etc.), then this will fallback to the default
         * behavior where the validation will not be performed and no DNSSEC results will be provided.
         *
         * If the zone is signed and there is a valid path to a known trust anchor configured in the system
         * and the application requires DNSSEC validation irrespective of the DNSSEC awareness in the current
         * network, then this option MUST not be used. This is only intended to be used during the transition
         * period where the different nodes participating in the DNS resolution may not understand DNSSEC or
         * managed properly (e.g. missing DS record) but still want to be able to resolve DNS successfully.
         */

        WakeOnlyService = 0x1000000,
        /*
         * This flag is meaningful only in DNSServiceRegister. When set, the service will not be registered
         * with sleep proxy server during sleep.
         */

        ThresholdOne = 0x2000000,
        ThresholdFinder = 0x4000000,
        ThresholdReached = ThresholdOne,
        /*
         * ThresholdOne is meaningful only in DNSServiceBrowse. When set,
         * the system will stop issuing browse queries on the network once the number
         * of answers returned is one or more.  It will issue queries on the network
         * again if the number of answers drops to zero.
         * This flag is for Apple internal use only. Third party developers
         * should not rely on this behavior being supported in any given software release.
         *
         * ThresholdFinder is meaningful only in DNSServiceBrowse. When set,
         * the system will stop issuing browse queries on the network once the number
         * of answers has reached the threshold set for Finder.
         * It will issue queries on the network again if the number of answers drops below
         * this threshold.
         * This flag is for Apple internal use only. Third party developers
         * should not rely on this behavior being supported in any given software release.
         *
         * When ThresholdReached is set in the client callback add or remove event,
         * it indicates that the browse answer threshold has been reached and no 
         * browse requests will be generated on the network until the number of answers falls
         * below the threshold value.  Add and remove events can still occur based
         * on incoming Bonjour traffic observed by the system.
         * The set of services return to the client is not guaranteed to represent the 
         * entire set of services present on the network once the threshold has been reached.
         *
         * Note, while ThresholdReached and ThresholdOne
         * have the same value, there  isn't a conflict because ThresholdReached
         * is only set in the callbacks and ThresholdOne is only set on
         * input to a DNSServiceBrowse call.
         */
        DenyCellular = 0x8000000,
        /*
         * This flag is meaningful only for Unicast DNS queries. When set, the kernel will restrict
         * DNS resolutions on the cellular interface for that request.
         */

        ServiceIndex = 0x10000000,
        /*
         * This flag is meaningful only for DNSServiceGetAddrInfo() for Unicast DNS queries.
         * When set, DNSServiceGetAddrInfo() will interpret the "interfaceIndex" argument of the call
         * as the "serviceIndex".
         */

        DenyExpensive = 0x20000000,
        /*
         * This flag is meaningful only for Unicast DNS queries. When set, the kernel will restrict
         * DNS resolutions on interfaces defined as expensive for that request.
         */

        PathEvaluationDone = 0x40000000
        /*
         * This flag is meaningful for only Unicast DNS queries.
         * When set, it indicates that Network PathEvaluation has already been performed.
         */

    }
}
