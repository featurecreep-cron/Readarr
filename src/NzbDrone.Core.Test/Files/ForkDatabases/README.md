Empty databases from Readarr and the forks users move between, as SQL dumps.
Each was made by building the fork's backend and starting it once on an empty data folder.

| File | Source | Commit |
|---|---|---|
| readarr-0.4.19.sql | Readarr/Readarr v0.4.19 | 0b79d3000d4e5f8425f499970b0190e2c421fceb |
| faustvii.sql | Faustvii/Readarr develop | 7f02ffa52997c92165c541a38be154054debc285 |
| ricetim.sql | ricetim/readarr-rresurrected develop | 0e956c9b13566ff03d291ae615a74c829002e8a5 |
| bookshelf-softcover.sql | pennydreadful/bookshelf develop, default image | 3ac41f7e31427684a37138e8e539b7e6c7b73869 |
| bookshelf-hardcover.sql | the same, with `METADATA_URL=https://hardcover.bookinfo.pro HARDCOVER=true` | 3ac41f7e31427684a37138e8e539b7e6c7b73869 |

The two bookshelf images share a schema; their seeded metadata profile differs.
