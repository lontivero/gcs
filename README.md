# gcs - Golomb compressed set

Library for creating and querying Golomb Compressed Sets (GCS), a statistical
compressed data-structure. The idea behind this implementation is using it in a new 
kind of Bitcoin light client, similar to SPV clients, that uses GCS instead of bloom 
filters.

This projects is based on the [original BIP (Bitcoin Improvement Proposal)](https://github.com/Roasbeef/bips/blob/master/gcs_light_client.mediawiki)
and [the Olaoluwa Osuntokun's reference implementation](https://github.com/Roasbeef/btcutil/tree/gcs/gcs)