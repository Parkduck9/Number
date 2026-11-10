const {decode}=require('./raster.cjs');
const path=require('path');
const a=decode(path.join(__dirname,'../boss_concept_4.png'));
a.crop(42,338,166,140).scale(4).save('review_v2/source_hand_4x.png');
a.crop(514,125,205,356).scale(2).save('review_v2/source_armor_2x.png');
