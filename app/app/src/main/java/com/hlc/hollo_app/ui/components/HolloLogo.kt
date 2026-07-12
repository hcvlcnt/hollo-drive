package com.hlc.hollo_app.ui.components

import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.hlc.hollo_app.R

@Composable
fun HolloLogo(modifier: Modifier = Modifier, markSize: Dp = 48.dp) {
    val markColor = MaterialTheme.colorScheme.onBackground
    Row(
        modifier = modifier.semantics { contentDescription = "Hollo" },
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Image(
            painter = painterResource(R.drawable.hollo_logo),
            contentDescription = null,
            modifier = Modifier.size(markSize),
        )
        Spacer(Modifier.width(12.dp))
        Text(
            text = "Hollo",
            color = markColor,
            fontSize = 28.sp,
            fontWeight = FontWeight.Bold,
            letterSpacing = (-0.6).sp,
        )
    }
}
